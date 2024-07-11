import { CompoundScrollKey, ItemInfo, ItemKey, OfferingKey, UpgradeScrollKey } from "typed-adventureland";

export class ItemUpgrade {
    name: ItemKey;
    max_level: number;
    max_grade: 0 | 1 | 2;

    constructor(name: ItemKey, max_level: number, max_grade?: 0 | 1 | 2) {
      this.name = name;
      this.max_level = max_level;
      this.max_grade = max_grade ?? 1;
    }

    find_upgrade_idx() : number | -1 {
        return character.items
            .map((item, i) => ({item, i}))
            .filter(x => this.does_item_match(x.item))
            .sort((a, b) => (a.item.level ?? 0) - (b.item.level ?? 0))
            .at(0)?.i ?? -1;
    }

    find_compound_indices() : number[] | null {
        const compoundable_items = character.items
            .map((item, i) => ({item, i}))
            .filter(x => this.does_item_match(x.item));

        const highest_level = compoundable_items.reduce((acc, x) => Math.max(acc, x.item.level ?? 0), 0);
        const lowest_level = compoundable_items.reduce((acc, x) => Math.min(acc, x.item.level ?? 0), 0);

        for (let i = highest_level; i >= lowest_level; --i) {
            const items = compoundable_items.filter(x => x.item.level == i);

            if (items.length >= 3) {
                return items.map(x => x.i);
            }
        }

        return null;
    }

    does_item_match(item: ItemInfo | null) : boolean {
        return item != null && 
            item.name == this.name && 
            item.p == null && 
            item.ach == null &&
            (item.level ?? 0) < this.max_level &&
            item_grade(item) <= this.max_grade;
    }
}

export class ItemUpgradeStrategy {
    scroll: UpgradeScrollKey;
    offering?: OfferingKey;

    constructor(scroll: UpgradeScrollKey, offering?: OfferingKey) {
        this.scroll = scroll;
        this.offering = offering;
    }
}

export const ARMOUR_UPGRADES : ItemUpgrade[] = [
    new ItemUpgrade("coat", 9),
    new ItemUpgrade("gloves", 9),
    new ItemUpgrade("helmet", 9),
    new ItemUpgrade("pants", 9),
    new ItemUpgrade("shoes", 9),
    new ItemUpgrade("mcape", 6),
    new ItemUpgrade("warmscarf", 5),
]

export const WEAPON_UPGRADES : ItemUpgrade[] = [
    new ItemUpgrade("t2bow", 8),
    new ItemUpgrade("basher", 7),
    new ItemUpgrade("crossbow", 7),
    new ItemUpgrade("firebow", 6),
    new ItemUpgrade("fireblade", 3, 2),
    new ItemUpgrade("firestaff", 3, 2),
]

export const EXPENSIVE_UPGRADES : ItemUpgrade[] = [
    new ItemUpgrade("wattire", 8, 2),
    new ItemUpgrade("wgloves", 9, 2),
    new ItemUpgrade("wcap", 9, 2),
    new ItemUpgrade("wbreeches", 8, 2),
    new ItemUpgrade("wshoes", 9, 2),

    new ItemUpgrade("cclaw", 8, 2),
    new ItemUpgrade("ecape", 8, 2),
    new ItemUpgrade("glolipop", 8, 2),
    new ItemUpgrade("quiver", 8, 2),

    new ItemUpgrade("harbringer", 7, 2),
    new ItemUpgrade("ololipop", 7, 2),
    new ItemUpgrade("oozingterror", 7, 2),
    new ItemUpgrade("sword", 7, 2),

    new ItemUpgrade("coat1", 8, 2),
    new ItemUpgrade("gloves1", 8, 2),
    new ItemUpgrade("helmet1", 8, 2),
    new ItemUpgrade("pants1", 8, 2),
    new ItemUpgrade("shoes1", 8, 2),

    new ItemUpgrade("cupid", 6, 2),
    new ItemUpgrade("firestars", 6, 2),
    new ItemUpgrade("merry", 6, 2),
    new ItemUpgrade("mittens", 6, 2),
    new ItemUpgrade("xmace", 6, 2),
    new ItemUpgrade("xmaspants", 6, 2),
    new ItemUpgrade("xmasshoes", 6, 2),
    new ItemUpgrade("xmassweater", 6, 2),

    new ItemUpgrade("t3bow", 4, 2),
]

export const UPGRADES : ItemUpgrade[] = [
    ...ARMOUR_UPGRADES,
    ...WEAPON_UPGRADES,
    ...EXPENSIVE_UPGRADES
];

const TIER1_UPGRADE_STRAT = {
    0: new ItemUpgradeStrategy("scroll1"),
    1: new ItemUpgradeStrategy("scroll1"),
    2: new ItemUpgradeStrategy("scroll1"),
    3: new ItemUpgradeStrategy("scroll1"),
    4: new ItemUpgradeStrategy("scroll1"),
    5: new ItemUpgradeStrategy("scroll1"),
    6: new ItemUpgradeStrategy("scroll1"),
    7: new ItemUpgradeStrategy("scroll2", "offeringp")
};

export const UPGRADE_STRATS : Partial<Record<ItemKey, Record<number, ItemUpgradeStrategy>>> = {
    "ololipop": {
        0: new ItemUpgradeStrategy("scroll0"),
        1: new ItemUpgradeStrategy("scroll0"),
        2: new ItemUpgradeStrategy("scroll1"),
        3: new ItemUpgradeStrategy("scroll1"),
        4: new ItemUpgradeStrategy("scroll1"),
        5: new ItemUpgradeStrategy("scroll1"),
        6: new ItemUpgradeStrategy("scroll1"),
        7: new ItemUpgradeStrategy("scroll1"),
        8: new ItemUpgradeStrategy("scroll2", "offeringp"),
        9: new ItemUpgradeStrategy("scroll2", "offeringp")
    },
    "coat1": TIER1_UPGRADE_STRAT,
    "gloves1": TIER1_UPGRADE_STRAT,
    "helmet1": TIER1_UPGRADE_STRAT,
    "pants1": TIER1_UPGRADE_STRAT,
    "shoes1": TIER1_UPGRADE_STRAT
}

export class ItemCompoundStrategy {
    scroll: CompoundScrollKey;
    offering?: OfferingKey;

    constructor(scroll: CompoundScrollKey, offering?: OfferingKey) {
        this.scroll = scroll;
        this.offering = offering;
    }
}

export const COMPOUNDS : ItemUpgrade[] = [
    new ItemUpgrade("strearring", 4),
    new ItemUpgrade("dexearring", 4),
    new ItemUpgrade("intearring", 4),
    new ItemUpgrade("dexamulet", 4),
    new ItemUpgrade("stramulet", 4),
    new ItemUpgrade("intamulet", 4),
    new ItemUpgrade("strbelt", 4),
    new ItemUpgrade("dexbelt", 4),
    new ItemUpgrade("intbelt", 4),
    new ItemUpgrade("wbook0", 4)
]

export const UPGRADES_AND_COMPOUNDS : ItemUpgrade[] = [
    ...UPGRADES,
    ...COMPOUNDS
]

const JEWELERY_UPGRADE_STRAT = {
    0: new ItemCompoundStrategy("cscroll0"),
    1: new ItemCompoundStrategy("cscroll1"),
    2: new ItemCompoundStrategy("cscroll1", "offeringp"),
    3: new ItemCompoundStrategy("cscroll2", "offeringp")
};

export const COMPOUND_STRATS : Partial<Record<ItemKey, Record<number, ItemCompoundStrategy>>> = {
    "strearring": JEWELERY_UPGRADE_STRAT,
    "dexearring": JEWELERY_UPGRADE_STRAT,
    "intearring": JEWELERY_UPGRADE_STRAT,
    "dexamulet": JEWELERY_UPGRADE_STRAT,
    "stramulet": JEWELERY_UPGRADE_STRAT,
    "intamulet": JEWELERY_UPGRADE_STRAT,
    "strbelt": JEWELERY_UPGRADE_STRAT,
    "dexbelt": JEWELERY_UPGRADE_STRAT,
    "intbelt": JEWELERY_UPGRADE_STRAT,
    "wbook0": JEWELERY_UPGRADE_STRAT
}

export const EXCHANGES: ItemKey[] = [
    "gem0",
    "goldenegg",
    "armorbox",
    "weaponbox",
    "candy0",
    "candy1",
    "basketofeggs",
    "candycane",
    "mistletoe",
	"seashell",
];

export const SELLS: ItemKey[] = [
    "hpamulet",
    "hpbelt",
    "whiteegg",
    "vitearring",
    "vitscroll",
    "wshield"
]

export const DESTROYS : ItemKey[] = [
    "carrotsword",
    "dagger",
    "eears",
    "epyjamas",
    "eslippers",
    "fireblade",
    "firestaff",
    "hpamulet",
	"hpbelt",
    "jacko",
    "phelmet",
    "pinkie",
    "pmace",
    "ringsj",
    "shield",
    "sshield",
    "slimestaff",
    "spear",
    "stinger",
];

export const DESTROYS_STRATS : Partial<Record<ItemKey, number>> = {
    "fireblade": 3,
    "firestaff": 3,
}

export const KEEP : ItemKey[] = [
    "tracker"
]

export const LUCKY_SLOT = 41;
