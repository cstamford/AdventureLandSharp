let _path: PIXI.Graphics[] = [];

async function pathfind_draw_path() {
    const response = await fetch("http://localhost:5201/Pathfinding/Path", {
        method: "POST",
        headers: { "accept": "application/json", "Content-Type": "application/json", },
        body: JSON.stringify({
            map: character.map,
            source: [ character.real_x, character.real_y ],
            dest: [ -174, -93 ],
        }),
    });

    const path = await response.json();    

    _path.forEach(x => x.destroy());
    _path = [];

    for (let i = 0; i < path.length; i++) {
        const from = path[i-1] ?? [ character.real_x, character.real_y ];
        const to = path[i];
        _path.push(draw_line(from[0], from[1], to[0], to[1], 1, 0x00FFFF));
    }

    setTimeout(pathfind_draw_path, 50);
}

async function pathfind_draw_grid() {
    const response = await fetch(`http://localhost:5201/Pathfinding/Grid/${character.map}`, {
        method: "GET",
        headers: { "accept": "application/json" }
    });

    const grid = await response.json();

    const cellSize = grid.cellSize;
    const width = grid.width;
    const height = grid.height;
    const terrainWalkableXY = grid.terrainWalkableXY;
    const terrainCostXY = grid.terrainCostXY;

    const e = new PIXI.Graphics();

    for (let x = 0; x < width; x++) {
        for (let y = 0; y < height; y++) {
            const walkable = terrainWalkableXY[x + y * width];
            const cost = terrainCostXY[x + y * width];
            const color = walkable ? 0x00FF00 : 0xFF0000;

            const x1 = G.maps[character.map].data.min_x + x*cellSize;
            const y1 = G.maps[character.map].data.min_y + y*cellSize;
            const x2 = x1 + cellSize;
            const y2 = y1 + cellSize;

            e.lineStyle(0.5, color, 0.33);
            e.moveTo(x1, y1);
            e.lineTo(x1, y2);
            e.lineTo(x2, y2);
            e.lineTo(x2, y1);
            e.lineTo(x1, y1);
        }
    }

    parent.map.addChild(e);  
    return e;
}

pathfind_draw_path();
pathfind_draw_grid();