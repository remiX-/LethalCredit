# 0.1.3 (beta.1.3)

- Updated QualityCompany dependency version + some terminal qol fixes
- Updated deposit hud notification text

# 0.1.2 (beta.1.2)

- Changed when Autobank occurs from SaveGame to DisplayDaysLeft due to scrap coming back after leaving during a moon
- Updated saving functionality to use the current game save file instead of a separate json file
  - **Note:** This will cause desync as it won't try fetch from old file format. Use `lcu-force` to re-update :)

# 0.1.1 (beta.1.1)

- Fixed bundle not being included in the mod package

# 0.1.0 (beta.1)

- Introduced your friendly bank, The Lethal Credit Union
- balance, deposit, withdraw and credit banking functionality
- Deposit scrap on the ship into the bank (less loot concept)
- Withdraw from the bank into some LCU buck
- Auto-bank scrap at end of game round
