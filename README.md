# ResonanceTools

ResonanceTools are a set of tools to parse and extract hotfix custom archives from the game [Resonance Solstice](https://resonance.ujoygames.com/#/)

## Tools:
1. **ResonanceTools.HotfixParser.exe** -> Parse `desc.bin` downloaded by the game from the hotfix server, which contains all the informations for downloading game files
2. **ResonanceTools.JABParser.exe** -> Can extract game assets from the game custom archives `.jab` used by the hotfix manager. Also can just save metadatas from the archive and save them in a `.json`

## Usage:
1. **ResonanceTools.HotfixParser.exe:**
```Usage: HotfixParser <input_file> <output_file>```
```Example: HotfixParser desc.bin output.json```
1. **ResonanceTools.JABParser.exe:**
```Usage: JABParser <file.jab>/<directory> [--extract <outDir>] [--buffer <size>] [--json <meta.json>]```
```Example: JABParser myfile.jab --extract outputDir```