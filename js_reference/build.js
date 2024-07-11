const { execSync } = require('child_process');
const fs = require("fs");
const http = require("http");

const file_descs = [
    { file: "upgrade", slot: 1},
    { file: "upgrade_crafted", slot: 2 }
];

for (const file_desc of file_descs) {
    const src_file = `src/${file_desc.file}.ts`;
    const out_file = `dist/${file_desc.file}.js`;
    const command = `npx esbuild ${src_file} --bundle --outfile=${out_file}`;
    console.log(`Executing: ${command}`);

    try {
        execSync(command, { stdio: 'inherit' });
        //uploadFile(out_file, file_desc.file, file_desc.slot);
    } catch (error) {
        console.error('Error during build:', error);
        process.exit(1);
    }
}

function uploadFile(code_path, code_desc, slot) {
    const code = fs.readFileSync(code_path);
    const req = http.request(
        {
            hostname: "adventure.land",
            path: "/api/save_code",
            method: "POST",
            headers: {
                Cookie: "CREDENTIAL_cookie",
                "Content-Type": "application/x-www-form-urlencoded",
            }
        },
        (res) => {
            res.on("data", (response) => {
                const asJson = JSON.parse(response.toString());
                console.log(`${code_path}: ${asJson[0].message}`);
            }
        );
      }
    );

    req.on("error", (err) => {
      console.error("Error talking to the AL API:", err);
    });

    const params = new URLSearchParams({
        method: "save_code",
        arguments: JSON.stringify({
            slot: slot.toString(),
            code: code.toString(),
            name: code_desc,
            log: "0",
        }),
    });

    req.write(params.toString());
    req.end();
  };