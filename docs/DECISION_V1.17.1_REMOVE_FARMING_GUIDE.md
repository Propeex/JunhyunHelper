# Decision — v1.17.1 Remove Farming Guide

Date: **2026-09-04 KST**  
Status: **CONFIRMED**

## 1. Decision

At the user's explicit request, Farming Guide is removed from 준현 헬퍼 as a product feature.

This removal is complete rather than a hidden/disabled mode.

## 2. Removed product surface

The product no longer includes:

- Farming Guide main navigation/page;
- loadout/inventory editor and presets;
- raid-session state, recommendation, global packing/repacking and loot optimization;
- Farming Guide locks, reserved cells, weight configuration and quantity-entry flow;
- Scanner → Farming Guide bridge;
- Mini Scanner Farming Guide instruction row;
- Farming Guide accept hotkey and related Scanner settings;
- simulated Farming Guide scan/test path;
- Farming Guide-specific persistence, services, domain policies and tests;
- Farming Guide-only Game Content metadata/import contracts.

## 3. Preserved product surface

The removal must not change the meaning or behavior of:

- Quest;
- Hideout;
- Items / Needed Items;
- Ammo;
- Map / MiniMap;
- Scanner recognition, search, Mini Scanner ordinary item information, correction, Ground Truth and diagnostics;
- Program Update and Game Content Update safety contracts.

## 4. Legacy user data

Existing historical `%LocalAppData%/JunhyunHelper/farming-guide.json` files are no longer read or written.

The application does not automatically delete those files. They are inert legacy user data, not an active product state.

Scanner settings written by older versions may contain removed Farming Guide JSON properties/order entries. Current settings normalization ignores/drops those obsolete entries without requiring user action.

## 5. Historical decisions

All prior Farming Guide decision documents remain repository history only.

They no longer define current product behavior and are superseded by this decision wherever they describe Farming Guide as an active feature.

## 6. Versioning

This is a change/removal of an existing feature, not the addition of a new product capability. Under `docs/VERSIONING.md`, the target version is **v1.17.1 PATCH**.

## 7. Verification

Release acceptance requires:

- no active Farming Guide implementation or navigation;
- no active Scanner Farming Guide integration/hotkey/display row;
- deterministic tests passing after Farming Guide-only coverage is removed;
- Windows Release build and self-contained publish;
- actual published EXE Product UI / Scanner / Map smoke;
- graceful shutdown and Shutdown Race;
- package/checksum validation;
- PR CI, exact-main CI and public release identity verification.
