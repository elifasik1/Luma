/**
 * Re-generate LUMA icons.
 * The source image (1024×682) has the icon centered.
 * We need to extract using the actual height as the square dimension.
 */
import sharp from "sharp";
import { writeFile, mkdir } from "node:fs/promises";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, "..");
const SOURCE = join(ROOT, "public", "luma-icon.png");

async function ensureDir(filePath) {
  await mkdir(dirname(filePath), { recursive: true });
}

async function main() {
  const meta = await sharp(SOURCE).metadata();
  console.log(`Source: ${meta.width}×${meta.height}`);

  // The icon is centered in a 1024×682 image.
  // The rounded square icon roughly occupies the center, bounded by the height.
  // Extract a square from the center using height as the dimension,
  // with slight padding reduction to crop closer to the icon edges.
  const squareSize = meta.height; // 682
  const left = Math.round((meta.width - squareSize) / 2); // (1024-682)/2 = 171

  console.log(`Extracting center square: ${squareSize}×${squareSize} at left=${left}`);

  const squareBuffer = await sharp(SOURCE)
    .extract({ left, top: 0, width: squareSize, height: squareSize })
    .toBuffer();

  // Now trim transparent/near-uniform edges to get tighter crop on the icon
  const trimmedBuffer = await sharp(squareBuffer)
    .trim({ threshold: 30 })
    .toBuffer();

  const trimmedMeta = await sharp(trimmedBuffer).metadata();
  console.log(`After trim: ${trimmedMeta.width}×${trimmedMeta.height}`);

  // Save preview
  await sharp(trimmedBuffer)
    .resize(256, 256, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png()
    .toFile(join(ROOT, "public", "luma-icon-cropped-preview.png"));
  console.log("✓ Saved crop preview (256×256)");

  // Generate all icon sizes
  const sizes = [
    { path: join(ROOT, "src", "app", "icon.png"), size: 32 },
    { path: join(ROOT, "src", "app", "apple-icon.png"), size: 180 },
    { path: join(ROOT, "public", "apple-icon.png"), size: 180 },
    { path: join(ROOT, "public", "icon-192.png"), size: 192 },
    { path: join(ROOT, "public", "icon-512.png"), size: 512 },
  ];

  for (const { path, size } of sizes) {
    await ensureDir(path);
    await sharp(trimmedBuffer)
      .resize(size, size, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png({ quality: 95 })
      .toFile(path);
    console.log(`✓ ${path} (${size}×${size})`);
  }

  // Generate ICO
  const icoSizes = [16, 32, 48];
  const pngBuffers = await Promise.all(
    icoSizes.map((size) =>
      sharp(trimmedBuffer)
        .resize(size, size, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
        .png()
        .toBuffer()
    )
  );

  const headerSize = 6;
  const dirEntrySize = 16;
  const numImages = pngBuffers.length;
  let dataOffset = headerSize + dirEntrySize * numImages;
  const dirEntries = [];

  for (let i = 0; i < numImages; i++) {
    const sz = icoSizes[i];
    const buf = pngBuffers[i];
    const entry = Buffer.alloc(dirEntrySize);
    entry.writeUInt8(sz < 256 ? sz : 0, 0);
    entry.writeUInt8(sz < 256 ? sz : 0, 1);
    entry.writeUInt8(0, 2);
    entry.writeUInt8(0, 3);
    entry.writeUInt16LE(1, 4);
    entry.writeUInt16LE(32, 6);
    entry.writeUInt32LE(buf.length, 8);
    entry.writeUInt32LE(dataOffset, 12);
    dirEntries.push(entry);
    dataOffset += buf.length;
  }

  const header = Buffer.alloc(headerSize);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(numImages, 4);

  const icoPath = join(ROOT, "src", "app", "favicon.ico");
  await ensureDir(icoPath);
  await writeFile(icoPath, Buffer.concat([header, ...dirEntries, ...pngBuffers]));
  console.log(`✓ ${icoPath} (ICO: ${icoSizes.join(", ")}px)`);

  console.log("\n✅ All icons regenerated!");
}

main().catch((err) => {
  console.error("Error:", err);
  process.exit(1);
});
