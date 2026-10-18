#!/usr/bin/env python3
"""Verify the exact package pin, unchanged recipes/guide and current consumer boundaries."""
import argparse, hashlib, json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--sdk-source',type=Path);parser.add_argument('--print-sdk-commit',action='store_true');args=parser.parse_args()
package=json.loads((ROOT/'Packages/manifest.json').read_text())['dependencies']['com.projectmakers.horizon']
match=re.fullmatch(r'https://github.com/ProjectMakersDE/horizOn-SDK-Unity.git#([0-9a-f]{40})',package);assert match,'Exact official Develop source SHA required'
commit=match.group(1)
if args.print_sdk_commit:print(commit);raise SystemExit(0)
lock=json.loads((ROOT/'Packages/packages-lock.json').read_text())['dependencies']['com.projectmakers.horizon'];assert lock['version']==package
folder=ROOT/'Assets/Scripts/OfficialFeatureRecipes';proof=json.loads((folder/'SOURCE.json').read_text());assert proof['commit']==commit
files={p.name for p in folder.glob('*.cs')};assert files==set(proof['files'])
for name,digest in proof['files'].items():
 content=(folder/name).read_bytes();assert hashlib.sha256(content).hexdigest()==digest,name
 if args.sdk_source:assert content==(args.sdk_source/'Samples~/Examples/Features'/name).read_bytes(),name
hello=ROOT/'Assets/Scripts/HelloFeatures';guide=hello/'MarketplaceDemo.cs';assert hashlib.sha256(guide.read_bytes()).hexdigest()=='2e236f514113bcb9934e13880b9f6be46481b7361cff5d477fa22411e4272afb'
for file in ['Assets/Scripts/Horizon/HorizonManager.cs','Assets/Scripts/UI/Components/GiftCodeForm.cs','Assets/Scripts/UI/SeagullInterface.cs']:
 source=(ROOT/file).read_text();assert 'GiftReward' not in source and '.giftData' not in source,file
 assert '.Redeem(code)' not in source and 'ValidateGiftCode(' not in source,file
assert (ROOT/'Assets/Scenes/HelloFeatures.unity').is_file()
guides=ROOT/'Assets/Scripts/PublicFeatureGuides';guide_proof=json.loads((guides/'SOURCE.json').read_text());assert guide_proof['sdk']==commit
for name,proof in guide_proof['files'].items():assert hashlib.sha256((guides/name).read_bytes()).hexdigest()==proof['sha256'],name
assert 'SeagullFeaturesSetup.Preflight' in (ROOT/'README.md').read_text(),'Document the actual native gate'
print(f'Exact SDK {commit}: {len(files)} unmodified existing-session recipes/helpers; unchanged public guide and V2 game/Hello boundaries verified')
