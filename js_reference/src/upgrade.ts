import { BankPackTypeItemsOnly, CompoundScrollKey, ItemInfo, MapKey, UpgradeScrollKey } from "typed-adventureland";
import * as cfg from "./upgrade_config";
import { assert, fast_craft, recover_loop } from "./util";

const DO_ITEM_UPGRADE = true;
const DO_ITEM_COMPOUND = true;
const DO_ITEM_SELL = true;
const DO_ITEM_DESTROY = true;
const DO_ITEM_EXCHANGE = false;

function is_upgrade_item(scroll: ItemInfo) {
    return scroll.name == "cscroll0" ||
        scroll.name == "cscroll1" ||
        scroll.name == "cscroll2" ||
        scroll.name == "cscroll3" ||
        scroll.name == "scroll0" ||
        scroll.name == "scroll1" ||
        scroll.name == "scroll2" ||
        scroll.name == "scroll3" ||
        scroll.name == "offering" ||
        scroll.name == "offeringp" ||
        scroll.name == "offeringx";
}

async function bank_loop() {
    const maps_to_tabs : Partial<Record<MapKey, BankPackTypeItemsOnly[]>> = {
        "bank": [ "items0", "items1", "items2", "items3", "items4", "items5", "items6", "items7" ],
        "bank_b": [ "items8", "items9", "items10", "items11", "items12", "items13", "items14", "items15", "items16", "items17", "items18", "items19", "items20", "items21", "items22", "items23" ],
        "bank_u": [ "items24", "items25", "items26", "items27", "items28", "items29", "items30", "items31", "items32", "items33", "items34", "items35", "items36", "items37", "items38", "items39" ],
    };

    const available_bank_tabs = Object.keys(character.bank!)
        .filter(x => x.startsWith("items"))
        .map(x => x as BankPackTypeItemsOnly)
        .filter(x => maps_to_tabs[character.map]?.includes(x))
        .map(x => ({tab: x, items: character.bank![x]}))
        .map(x => ({...x, available_slots: x.items.filter(y => y == null).length}));

    const items_to_deposit = character.items
        .map((item, idx) => ({item, idx}))
        .filter(x => x.item != null)
        .filter(x => !is_upgrade_item(x.item) &&
            !cfg.KEEP.some(y => y == x.item?.name) &&
            !cfg.UPGRADES.some(y => y.does_item_match(x.item)) &&
            !(cfg.COMPOUNDS.some(y => y.does_item_match(x.item)) && character.items.filter(i => i?.name == x.item?.name && i?.level == x.item?.level).length >= 3));

    for (const item_to_deposit of items_to_deposit) {
        const bank_tab = 
            // tab that already has this item
            available_bank_tabs
                .filter(x => 
                    (x.available_slots > 0 || item_to_deposit.item.q) &&
                    x.items.findIndex(x => x && x.name == item_to_deposit.item.name && x.p == null) != -1)
                .at(0)?.tab ??
            // next available free tab
            available_bank_tabs.find(x => x.available_slots > 0)?.tab;

        game_log(`Depositing ${item_to_deposit.item.name} in ${bank_tab}`, "green");
        await bank_store(item_to_deposit.idx, bank_tab);
    }

    const num_free_slots = 42 - character.items.filter(i => i).length - 8;
    if (num_free_slots <= 0) {
        return false;
    }

    const upgrade_items_to_withdraw = available_bank_tabs
        .map(tab => ({tab: tab.tab, items: tab.items
            .map((item, idx) => ({item, idx}))
            .filter(item => item.item && is_upgrade_item(item.item))}))
        .flatMap(tab => tab.items.map(x => ({item: x.item!, tab: tab.tab, tabIdx: x.idx})));

    var itemIdx = 0;
    var upgradeWithdrawIdx = 0;
    while (itemIdx < num_free_slots && upgradeWithdrawIdx < upgrade_items_to_withdraw.length) {
        const cur = upgrade_items_to_withdraw[upgradeWithdrawIdx++];
        game_log(`Withdrawing ${cur.item.name} from ${cur.tab}`, "green");
        await bank_retrieve(cur.tab, cur.tabIdx);
    }

    const items_to_withdraw = available_bank_tabs
        .map(tab => ({tab: tab.tab, items: tab.items
            .map((item, idx) => ({item, idx, upgradeIdx: cfg.UPGRADES_AND_COMPOUNDS.findIndex(y => (item?.level ?? 0) < y.max_level && y.does_item_match(item))}))
            .filter(x => x.item != null && x.upgradeIdx != -1)}))
        .flatMap(tab => tab.items.map(x => ({item: x.item!, tab: tab.tab, tabIdx: x.idx, upgradeIdx: x.upgradeIdx})))
        .sort((a, b) => a.upgradeIdx == b.upgradeIdx ? ((b.item?.level ?? 0) - (a.item?.level ?? 0)) : (b.upgradeIdx - a.upgradeIdx));

    var withdrawIdx = 0;
    while (itemIdx < num_free_slots && withdrawIdx < items_to_withdraw.length) {
        const cur = items_to_withdraw[withdrawIdx++];
        const is_compound = cfg.COMPOUNDS.findIndex(x => x.does_item_match(cur.item)) != -1;

        if (is_compound) {
            if (itemIdx + 2 >= num_free_slots) {
                continue;
            }

            const next = items_to_withdraw.at(withdrawIdx);
            const next_two = items_to_withdraw.at(withdrawIdx + 1);

            if (!next ||
                next.upgradeIdx != cur.upgradeIdx ||
                next.item.level != cur.item.level ||
                !next_two ||
                next_two.upgradeIdx != cur.upgradeIdx ||
                next_two.item.level != cur.item.level)
            {
                continue;
            }

            await bank_retrieve(cur.tab, cur.tabIdx);
            await bank_retrieve(next.tab, next.tabIdx);
            await bank_retrieve(next_two.tab, next_two.tabIdx);
            itemIdx += 3;
        } else {
            await bank_retrieve(cur.tab, cur.tabIdx);
            itemIdx += 1;
        }
    }

    return items_to_withdraw.length == 0 && items_to_deposit.length == 0;
}

async function upgrade_loop() {
    function item_sellable(item: ItemInfo, level = 0) {
        return item && !item.p && !item.ach && !item.q && (item.level ?? 0) == level;
    }

    const item_destroy = cfg.DESTROYS
        .map(destroy => ({idx: locate_item(destroy)}))
        .filter(x => x.idx != -1)
        .map(x => ({...x, item: character.items[x.idx]}))
        .filter(x => item_sellable(x.item, cfg.DESTROYS_STRATS[x.item.name] ?? 0))
        .at(0);

    const item_sell = cfg.SELLS
        .map(sell => ({idx: locate_item(sell)}))
        .filter(x => x.idx != -1)
        .map(x => ({...x, item: character.items[x.idx]}))
        .filter(x => item_sellable(x.item))
        .at(0);

    const item_upgrade = cfg.UPGRADES
        .map(item => ({item, idx: item.find_upgrade_idx()}))
        .filter(x => x.idx != -1)
        .at(0);

    const item_compound = cfg.COMPOUNDS
        .map(item => ({item, idx: item.find_compound_indices()}))
        .filter(x => x.idx != null)
        .at(0);

    const item_exchange = cfg.EXCHANGES
        .map(exchange => ({idx: locate_item(exchange)}))
        .filter(x => x.idx != -1)
        .map(x => ({...x, item: character.items[x.idx]}))
        .at(0);

    if (DO_ITEM_DESTROY && item_destroy) {
        game_log(`Destroying ${item_destroy.item.name}`, "green");
        await destroy(item_destroy.idx);
    } else if (DO_ITEM_SELL && item_sell) {
        game_log(`Selling ${item_sell.item.name}`, "green");
        await sell(item_sell.idx);
    } else if (DO_ITEM_UPGRADE && item_upgrade) {
        const item = character.items[item_upgrade.idx];
        const grade = item_grade({...item, level: item.level});
        assert(grade != -1, "Item is not upgradable");

        const offering = cfg.UPGRADE_STRATS[item.name]?.[item.level ?? 0]?.offering;
        const offering_idx = offering ? locate_item(offering) : null;
        assert(!offering || (offering_idx && offering_idx >= 0), "Missing offering item");

        const scroll: UpgradeScrollKey = (cfg.UPGRADE_STRATS[item.name]?.[item.level ?? 0]?.scroll) ?? `scroll${grade}`;
        let scroll_idx = locate_item(scroll);

        if (scroll_idx == -1) {
            const result = await buy(scroll, 5);
            scroll_idx = result.num;
        }

        assert(scroll_idx >= 0, "Missing scroll item");

        while (scroll_idx == item_upgrade.idx || scroll_idx == cfg.LUCKY_SLOT) {
            const next_scroll_idx = (scroll_idx + 1) % 42;
            await swap(next_scroll_idx, scroll_idx);
            scroll_idx = next_scroll_idx;
        }

        if (item_upgrade.idx != cfg.LUCKY_SLOT) {
            await swap(cfg.LUCKY_SLOT, item_upgrade.idx);
            item_upgrade.idx = cfg.LUCKY_SLOT;
        }

        game_log(`Upgrading ${item_upgrade.item.name} from level ${item.level} to ${(item.level??0) + 1} with scroll:${scroll}, offering:${offering ?? "none"}`, "green");

        await fast_craft(item_upgrade.idx);
        await upgrade(item_upgrade.idx, scroll_idx, offering_idx);
    } else if (DO_ITEM_COMPOUND && item_compound) {
        game_log(`Compounding ${item_compound.item.name} to level ${item_compound.item.max_level}`, "green");

        const first_item = character.items[item_compound.idx![0]];
        const grade = item_grade({...first_item, level: first_item.level});
        assert(grade != -1 && grade != 4, "Item is not compoundable");

        const offering = cfg.COMPOUND_STRATS[first_item.name]?.[first_item.level ?? 0]?.offering;
        const offering_idx = offering ? locate_item(offering) : null;
        assert(!offering || (offering_idx && offering_idx >= 0), "Missing offering item");

        const scroll: CompoundScrollKey = (cfg.COMPOUND_STRATS[first_item.name]?.[first_item.level ?? 0]?.scroll) ?? `cscroll${grade}`;
        let scroll_idx = locate_item(scroll);

        if (scroll_idx == -1) {
            const result = await buy(scroll, 5);
            scroll_idx = result.num;
        }

        assert(scroll_idx >= 0, "Missing scroll item");

        await fast_craft(item_compound.idx![0]);
        await compound(item_compound.idx![0], item_compound.idx![1], item_compound.idx![2], scroll_idx, offering_idx);
    } else if (DO_ITEM_EXCHANGE && item_exchange) {
        game_log(`Exchanging ${item_exchange.item.name}`, "green");
        await exchange(item_exchange.idx);
    }

    return (DO_ITEM_DESTROY && item_destroy) || 
        (DO_ITEM_SELL && item_sell) || 
        (DO_ITEM_UPGRADE && item_upgrade) || 
        (DO_ITEM_COMPOUND && item_compound) || 
        (DO_ITEM_EXCHANGE && item_exchange);
}

async function main() {
    try {
        if (character.map == "bank" || character.map == "bank_b" || character.map == "bank_u") {
            if (!await bank_loop()) {
                if (character.map == "bank") {
                    await smart_move("bank_b");
                } else if (character.map == "bank_b") {
                    await smart_move("upgrade");
                }
            }
        } else {
            if (!await upgrade_loop()) {
                await smart_move("bank");
            }
        }
    } catch (e: any) {
        game_log(`${e.message} ${e.stack}`, "red");
    }

    const fullInventory = character.items.filter(i => i).length >= 42;
    setTimeout(main, fullInventory ? 1000 : 0);
}

main();
recover_loop();
