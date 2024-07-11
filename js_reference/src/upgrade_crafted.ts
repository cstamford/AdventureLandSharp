import { ItemKey, UpgradeScrollKey } from "typed-adventureland";
import * as cfg from "./upgrade_config";
import { assert, fast_craft, recover_loop } from "./util";

async function upgrade_loop() {
    const count_per_round = 25;
    const base_items_buy: ItemKey[] = [ "blade" ]
    const base_items_nobuy: ItemKey[] = [ "essenceoffire" ];
    const base_items = [...base_items_buy, ...base_items_nobuy];
    const crafted_item: ItemKey = "fireblade";
    const upgraded_item = new cfg.ItemUpgrade(crafted_item, 3);
    const delete_item = true;
    const buy_pos = { x: -18, y: 225 };
    const buy_scrolls_pos = { x: -220, y: 100 };
    const dismantle_pos = { x: -18, y: 290 };

    if (character.items.findIndex(i => upgraded_item.does_item_match(i)) == -1) {
        const buys = [];

        for (const base_item of base_items_buy) {
            const base_item_count = character.items.filter(i => i && i.name == base_item).length;
            const base_items_to_buy = count_per_round - base_item_count;
            if (base_items_to_buy > 0) {
                await smart_move(buy_pos);
                for (let i = 0; i < base_items_to_buy; ++i) {
                    buys.push(buy(base_item, base_items_to_buy));
                }
            }
        }

        await Promise.all(buys);

        const crafts = [];
        const indices_used: number[] = [];

        var dismantle_moved = false;

        while (true) {
            const indices = [];
            const indices_per_round = base_items.length;

            game_log(`Indices used: ${indices_used.join(", ")}`);

            for (var i = 0; i < 42; ++i) {
                if (character.items[i] == null || (character.items[i].q == null && 
                    (indices_used.includes(i) || indices.findIndex(x => character.items[x].name == character.items[i].name) != -1))) 
                {
                    continue;
                }

                if (base_items.includes(character.items[i].name)) {
                    game_log(`Found base item ${character.items[i].name} at index ${i}`);
                    indices.push(i);
                }

                if (indices.length == indices_per_round) {
                    break;
                }
            }

            game_log(`Using indices ${indices.join(", ")} for crafting`);
            game_log(`indices.length: ${indices.length}, indices_per_round: ${indices_per_round}`);

            if (indices.length < indices_per_round) {
                break;
            }

            game_log(`Crafting ${indices.map(i => character.items[i].name).join(", ")}`);

            if (!dismantle_moved) {
                await smart_move(dismantle_pos);
                dismantle_moved = true;
            }

            crafts.push(craft(
                indices[0],
                indices[1],
                indices[2],
                indices[3],
                indices[4],
                indices[5],
                indices[6],
                indices[7],
                indices[8])
            );

            indices_used.push(...indices);
        }

        await Promise.all(crafts);
    }

    if (upgraded_item.max_level == 13) {
        const destroys = []

        for (let i = 0; i < 42; ++i) {
            if (character.items[i] == null || 
                character.items[i].name != crafted_item ||
                (character.items[i].level ?? 0) > 0 || 
                character.items[i].p != null)
            {
                continue;
            }

            if (upgraded_item.does_item_match(character.items[i])) {
                destroys.push(destroy(i));
            }
        }

        await Promise.all(destroys);
        return;
    }

    let upgrade_idx = upgraded_item.find_upgrade_idx();
    if (upgrade_idx == -1) {
        return;
    }

    if (upgrade_idx != cfg.LUCKY_SLOT) {
        await swap(cfg.LUCKY_SLOT, upgrade_idx);
        upgrade_idx = cfg.LUCKY_SLOT;
    }

    if (simple_distance(character, buy_scrolls_pos) > 10) {
        await smart_move(buy_scrolls_pos);
    }

    const item = character.items[upgrade_idx];
    const grade = item_grade({...item, level: item.level});
    assert(grade != -1, "Item is not upgradable");

    const scroll: UpgradeScrollKey = `scroll${grade}`;
    let scroll_idx = locate_item(scroll);

    if (scroll_idx == -1) {
        const result = await buy(scroll, count_per_round);
        scroll_idx = result.num;
    }

    await fast_craft(upgrade_idx);
    const upgrade_result = await upgrade(upgrade_idx, scroll_idx);

    if (upgrade_result.success && character.items[upgrade_result.num].level == upgraded_item.max_level && delete_item) {
        await destroy(upgrade_result.num);
    }
}

async function main() {
    try {
        await upgrade_loop();
    } catch (e: any) {
        game_log(`${e.message} ${e.stack}`, "red");
    }

    const fullInventory = character.items.filter(i => i).length >= 42;
    setTimeout(main, fullInventory ? 1000 : 100);
}

main();
recover_loop();