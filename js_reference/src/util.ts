export function assert(condition: any, message: string): asserts condition {
    if (!condition) {
        throw new Error(message);
    }
}

export async function fast_craft(craftIdx: number) {
    const useFastCraft = character.mp >= 500 && (character.items[craftIdx].level ?? 0) >= 3 || character.mp >= character.max_mp - 500;

    if (can_use("massproductionpp") && useFastCraft) {
        await use_skill("massproductionpp");
    } else if (can_use("massproduction")) {
        await use_skill("massproduction");
    }
}

export async function recover() {
    var usePassive = true;
    if (character.mp <= character.max_mp - 500) {
        try {
            var potIdx = locate_item("mpot1");
            if (potIdx == -1) {
                potIdx = (await buy("mpot1", 1)).num;
            }
            equip(potIdx);   
            usePassive = false; 
        } catch {} 
    }

    if (usePassive) {
        use_hp_or_mp();
    }
}

export async function recover_loop() {
    await recover();
    setTimeout(recover_loop, 2000);
}
