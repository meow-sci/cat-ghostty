import { XXH64 } from 'xxh3-ts';
import { Buffer } from 'buffer';


export function HashComp() {

    let hash1: bigint = XXH64(Buffer.from("color: #ffffff;\ncursor:pointer;"));
    let hash2: bigint = XXH64(Buffer.from("color: #ffffff;\ncursor:pointer;"));
    let hash3: bigint = XXH64(Buffer.from("color: #00ffff;\ncursor:pointer;"));

    console.log(`hash1: ${hash1}`);
    console.log(`hash2: ${hash2}`);
    console.log(`hash3: ${hash3}`);

    return (
        <>
            <p>hash1: {hash1}</p>
            <p>hash2: {hash2}</p>
            <p>hash3: {hash3}</p>

        </>
    )

}