export {};
declare global {
    function send_mail(to: string, subject: string, message: string, item: boolean): any;
}

async function main() {
    while (true) {
        var earringIdx = character.items.findIndex(item =>
            item != null && 
            (item.level ?? 0) >= 1 && (
            item.name == "strearring" ||
            item.name == "dexearring" ||
            item.name == "intearring"));

        if (earringIdx == -1) {
            game_log("No earrings found", "red");
            break;
        }

        if (earringIdx != 0) {
            game_log(`Swapping ${earringIdx} to slot 0`, "green");
            await swap(0, earringIdx);
            earringIdx = 0;
        }

        const recipient = "Diocles";
        const item = character.items[earringIdx];
        const label = `${item.name} ${item.level}`;

        game_log(`Sending ${label} to ${recipient}`, "green");
        await send_mail("Diocles", label, label, true);
        game_log(`Sent ${label} to ${recipient}`, "green");
    }
}

main();
