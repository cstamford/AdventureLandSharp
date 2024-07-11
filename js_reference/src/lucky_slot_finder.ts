import { ServerToClient_q_data, ShieldKey, UpgradeScrollKey } from "typed-adventureland";
import { assert } from "./util";

const _redis: string = "CREDENTIAL_webdis_url";
const _scroll: UpgradeScrollKey = "scroll0";
const _shield: ShieldKey = "wshield";

const _slot_whitelist = [
    0, 8, 41,
    /*i
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 
    10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 
    20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 
    30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 
    40, 41
    */
];

async function submit_result(slot: number, roll: number) {
    await fetch(_redis, {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded"
        },
        body: `LPUSH/lucky_slot_finder/${encodeURIComponent(JSON.stringify({slot, roll}))}`
      });
}

async function refresh_shields() {
    const shield_sells = [];
    for (const slot of character.items
        .map((x, i) => ({x, i}))
        .filter(x => x.x?.name == _shield)
        .map(x => x.i)
    ) {
        shield_sells.push(sell(slot));
    }

    await Promise.all(shield_sells);

    const shield_buys = [];
    for (let i = 0; i < _slot_whitelist.length; ++i) {
        shield_buys.push(buy(_shield));
    }

    await Promise.all(shield_buys);
}

async function refresh_scrolls() {
    const scroll_count = _slot_whitelist.length * 2;
    let scroll_idx = locate_item(_scroll);

    if (scroll_idx == -1) {
        const result = await buy(_scroll, scroll_count);
        scroll_idx = result.num;
    }

    const current_scroll_count = character.items[scroll_idx].q ?? 1;
    if (current_scroll_count < scroll_count) {
        await buy(_scroll, scroll_count - current_scroll_count);
    }
}

async function upgrade_with_nums(slot: number, scroll_slot: number): Promise<number[]> {
    const nums_promise = new Promise<number[]>((resolve, reject) => {
        assert(parent.socket.listeners("q_data").length == 1, "q_data listener already exists");

        const fn_listener = (data: ServerToClient_q_data) => {
            game_log(`${slot} ${data.num} ${data.p.nums}`);
            if (data.num == slot && data.p.nums.length == 4 && data.p.nums.every(x => x != undefined)) {
                try {
                    resolve([...data.p.nums]);
                } finally {
                    parent.socket.removeListener("q_data", fn_listener);
                }
            }
        };

        parent.socket.on("q_data", fn_listener);

        setTimeout(() => {
            reject("Timed out");
            parent.socket.removeListener("q_data", fn_listener);
        }, 5000);
    });

    const _ = await upgrade(slot, scroll_slot);
    return await nums_promise;
}

async function main_impl() {
    await refresh_shields();
    await refresh_scrolls();

    const submits = [];

    for (const slot of _slot_whitelist) {
        if (character.items[slot] == null ||
            character.items[slot].name != _shield)
        {
            const shield_slot = locate_item(_shield);
            assert(shield_slot != -1, "Shield not found");
            await swap(slot, shield_slot);
        }

        const scroll_slot = locate_item(_scroll);
        assert(scroll_slot != -1, "Scroll not found");

        const nums = await upgrade_with_nums(slot, scroll_slot);
        const value = nums[0] * 10000 + nums[1] * 1000 + nums[2] * 100 + nums[3] * 10;
        submits.push(submit_result(slot, value));
    }

    await Promise.all(submits);
}

async function main() {
    try {
        await main_impl();
    } catch (e: any) {
        game_log(`${e.message} ${e.stack}`, "red");
    }

    setTimeout(main, 0);
}

main();
