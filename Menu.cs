﻿using MenuLib.MonoBehaviors;
using MenuLib;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using System.Xml.Linq;
using RepoAdminMenu.Utils;

namespace RepoAdminMenu {
    internal class Menu {

        private static string selectedPlayerId;

        private static REPOPopupPage currentMenu;

        private static string currentMenuStr = "";

        private static Dictionary<string, Action> menus = new Dictionary<string, Action>();

        private static MethodInfo removeAllPagesMethod = AccessTools.Method(typeof(MenuManager), "PageCloseAll");

        private static Dictionary<string, Action<string, REPOPopupPage>> menuPreCallbacks = new Dictionary<string, Action<string, REPOPopupPage>>();
        private static Dictionary<string, Action<string, REPOPopupPage>> menuPostCallbacks = new Dictionary<string, Action<string, REPOPopupPage>>();

        public static void Init() {
            registerMenu("playerList", openPlayerListMenu);
            registerMenu("player", openPlayerMenu);
            registerMenu("playerUpgrade", openPlayerUpgrades);
            registerMenu("spawn", openSpawnMenu);
            registerMenu("spawnItem", openSpawnItemsMenu);
            registerMenu("spawnEnemy", openSpawnEnemyMenu);
            registerMenu("spawnValuable", openSpawnValuablesMenu);
            registerMenu("spawnValuableTiny", openSpawnValuablesTinyMenu);
            registerMenu("spawnValuableSmall", openSpawnValuablesSmallMenu);
            registerMenu("spawnValuableMedium", openSpawnValuablesMediumMenu);
            registerMenu("spawnValuableBig", openSpawnValuablesBigMenu);
            registerMenu("spawnValuableWide", openSpawnValuablesWideMenu);
            registerMenu("spawnValuableTall", openSpawnValuablesTallMenu);
            registerMenu("spawnValuableVeryTall", openSpawnValuablesVeryTallMenu);
            registerMenu("map", openMapMenu);
            registerMenu("levelSelector", openLevelSelectorMenu);
            registerMenu("settings", openSettingsMenu);
            registerMenu("credits", openCreditsMenu);
            registerMenu("mainmenu", openMainMenu);
        }

        public static void registerMenu(string name, Action openAction) {
            menus.Add(name, openAction);
        }

        public static void addMenuPreCallback(string mod_name, Action<string, REPOPopupPage> action) {
            menuPreCallbacks.Add(mod_name, action);
        }

        public static void removeMenuPreCallback(string mod_name) {
            menuPreCallbacks.Remove(mod_name);
        }

        public static void addMenuPostCallback(string mod_name, Action<string, REPOPopupPage> action) {
            menuPostCallbacks.Add(mod_name, action);
        }

        public static void removeMenuPostCallback(string mod_name) {
            menuPostCallbacks.Remove(mod_name);
        }

        public static void toggleMenu() {
            if (menus.Count < 1)
                Init();


            if (SemiFunc.MenuLevel()) {
                RepoAdminMenu.mls.LogInfo("Repo Admin Menu can only be opened while in-game!");
                return;
            }

            if (!SemiFunc.IsMasterClientOrSingleplayer()) {
                RepoAdminMenu.mls.LogInfo("Repo Admin Menu can only be opened in single player or as host in multiplayer!");
                return;
            }

            if (currentMenu != null && currentMenu.isActiveAndEnabled) {
                closePage(currentMenu);
            } else {
                RepoAdminMenu.mls.LogInfo("Opening menu");
                menus.GetValueOrDefault(currentMenuStr, openMainMenu).Invoke();
            }
        }

        public static void closePage(REPOPopupPage page) {
            RepoAdminMenu.mls.LogInfo("Closing: " + page.menuPage.name);
            removeAllPagesMethod.Invoke(MenuManager.instance, new object[] { });
            page.ClosePage(true);
            MenuManager.instance.PageRemove(page.menuPage);
            if (currentMenu == page)
                currentMenu = null;
        }

        public static void navigate(REPOPopupPage page, string menu) {
            closePage(page);
            menus.GetValueOrDefault(menu, () => { RepoAdminMenu.mls.LogError("Menu not found: " + menu);  }).Invoke();
        }

        public static void openPage(REPOPopupPage page, string name) {
            MenuManager.instance.StartCoroutine(openPageInternal(page, name)); 
            
        }


        private static System.Collections.IEnumerator openPageInternal(REPOPopupPage page, string name) {
            yield return new WaitForSeconds(0.050f);
            foreach (KeyValuePair<string, Action<string, REPOPopupPage>> entry in menuPostCallbacks) {
                RepoAdminMenu.mls.LogInfo("Running post-callback for '" + entry.Key + "' on menu '" + name + "'");
                entry.Value.Invoke(name, page);
            }
            RepoAdminMenu.mls.LogInfo("Opening: " + page.menuPage.name);
            removeAllPagesMethod.Invoke(MenuManager.instance, new object[] { });
            page.OpenPage(false);
            currentMenu = page;
            currentMenuStr = name;
        }

        public static REPOPopupPage createMenu(string title, string currentMenu, string parentMenu) {
            var elem = MenuAPI.CreateREPOPopupPage(title, REPOPopupPage.PresetSide.Left, false, true);
            foreach (KeyValuePair<string, Action<string, REPOPopupPage>> entry in menuPreCallbacks) {
                RepoAdminMenu.mls.LogInfo("Running pre-callback for '" + entry.Key + "' on menu '" + currentMenu + "'");
                entry.Value.Invoke(currentMenu, elem);
            }
            addBackButton(elem, parentMenu);
            return elem;
        }

        public static REPOPopupPage createMainMenu(string title) {
            var elem = MenuAPI.CreateREPOPopupPage(title, REPOPopupPage.PresetSide.Left, false, true);
            foreach (KeyValuePair<string, Action<string, REPOPopupPage>> entry in menuPreCallbacks) {
                RepoAdminMenu.mls.LogInfo("Running pre-callback for '" + entry.Key + "' on menu 'mainmenu'");
                entry.Value.Invoke("mainmenu", elem);
            }
            addCloseButton(elem);
            return elem;
        }

        public static void addButton(REPOPopupPage parent, string text, Action action) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOButton(text, action, scrollView);
                return elem.rectTransform;
            });
        }

        public static void addBackButton(REPOPopupPage parent, string parentMenu) {
            parent.AddElement(transform => {
                MenuAPI.CreateREPOButton("← Back", () => { closePage(parent); menus.GetValueOrDefault(parentMenu, () => { RepoAdminMenu.mls.LogError("Menu not found: " + parentMenu); }).Invoke(); }, transform, new Vector2(250, 20));
            });
        }

        public static void addCloseButton(REPOPopupPage parent) {
            parent.AddElement(transform => {
                MenuAPI.CreateREPOButton("Close", () => closePage(parent), transform, new Vector2(270, 20));
            });
        }


        public static void addToggle(REPOPopupPage parent, string text, System.Action<bool> action, string off, string on, bool defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOToggle(text, action, scrollView, Vector2.zero, off, on, defaultValue);
                return elem.rectTransform;
            });
        }
        public static void addLabel(REPOPopupPage parent, string text) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOLabel(text, scrollView);
                return elem.rectTransform;
            });
        }

        public static void addIntSlider(REPOPopupPage parent, string text, string description, System.Action<int> action, int min, int max, int defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOSlider(text, description, action, scrollView, Vector2.zero, min, max, defaultValue, "", "", REPOSlider.BarBehavior.UpdateWithValue);
                return elem.rectTransform;
            });
        }
        public static void addFloatSlider(REPOPopupPage parent, string text, string description, System.Action<float> action, float min, float max, int precision, float defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOSlider(text, description, action, scrollView, Vector2.zero, min, max, precision, defaultValue, "", "", REPOSlider.BarBehavior.UpdateWithValue);
                return elem.rectTransform;
            });
        }

        public static void addStringSlider(REPOPopupPage parent, string text, string description, System.Action<string> action, string[] options, string defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOSlider(text, description, action, scrollView, options, defaultValue, Vector2.zero, "", "", REPOSlider.BarBehavior.UpdateWithValue);
                return elem.rectTransform;
            });
        }

        public static void openMainMenu() {
            var mainMenu = createMainMenu("R.E.P.O. Admin Menu");

            addButton(mainMenu, "Players", () => { navigate(mainMenu, "playerList"); });
            addButton(mainMenu, "Spawn", () => { navigate(mainMenu, "spawn"); });
            addButton(mainMenu, "Map", () => { navigate(mainMenu, "map"); });
            addButton(mainMenu, "Settings", () => { navigate(mainMenu, "settings"); });
            addButton(mainMenu, "Credits", () => { navigate(mainMenu, "credits"); });
            addButton(mainMenu, "Report a Bug", () => { Application.OpenURL("https://github.com/proferabg/RepoAdminMenu/issues"); });

            openPage(mainMenu, "mainmenu");
        }

        private static void openPlayerListMenu() {
            var playersMenu = createMenu("R.A.M. - Players", "playerList", "mainmenu");

            foreach (PlayerAvatar player in SemiFunc.PlayerGetAll()) {
                addButton(playersMenu, SemiFunc.PlayerGetName(player), () => { selectedPlayerId = SemiFunc.PlayerGetSteamID(player); navigate(playersMenu, "player"); });
            }

            openPage(playersMenu, "playerList");
        }

        private static void openPlayerMenu() {
            if (selectedPlayerId == null) {
                RepoAdminMenu.mls.LogInfo("No player selected");
                openPlayerListMenu();
                return;
            }

            PlayerAvatar avatar = SemiFunc.PlayerGetFromSteamID(selectedPlayerId);

            var playerMenu = createMenu("R.A.M. - " + SemiFunc.PlayerGetName(avatar), "player", "playerList");

            addToggle(playerMenu, "God Mode", (b) => { PlayerUtil.toggleGodMode(b, avatar); }, "Off", "On", !PlayerUtil.isGod(avatar));
            addToggle(playerMenu, "No Death", (b) => { PlayerUtil.toggleNoDeath(b, avatar); }, "Off", "On", !PlayerUtil.isNoDeath(avatar));
            addToggle(playerMenu, "No Target", (b) => { PlayerUtil.toggleNoTarget(b, avatar); }, "Off", "On", !PlayerUtil.isNoTarget(avatar));
            addToggle(playerMenu, "No Tumble", (b) => { PlayerUtil.toggleNoTumble(b, avatar); }, "Off", "On", !PlayerUtil.isNoTumble(avatar));
            addToggle(playerMenu, "Infinite Stamina", (b) => { PlayerUtil.toggleInfiniteStamina(b, avatar); }, "Off", "On", !PlayerUtil.isInfiniteStamina(avatar));
            addButton(playerMenu, "Upgrades", () => { navigate(playerMenu, "playerUpgrade"); });
            addButton(playerMenu, "Heal", () => { PlayerUtil.healPlayer(avatar); });
            addButton(playerMenu, "Kill", () => { PlayerUtil.killPlayer(avatar); });
            addButton(playerMenu, "Revive", () => { PlayerUtil.revivePlayer(avatar); });
            addButton(playerMenu, "Give Crown", () => { PlayerUtil.giveCrown(avatar); });

            openPage(playerMenu, "player");
        }

        private static void openPlayerUpgrades() {
            if (selectedPlayerId == null) {
                RepoAdminMenu.mls.LogInfo("No player selected");
                openPlayerListMenu();
                return;
            }

            PlayerAvatar avatar = SemiFunc.PlayerGetFromSteamID(selectedPlayerId);

            var upgradesMenu = createMenu("R.A.M. - " + SemiFunc.PlayerGetName(avatar) + " - Upgrades", "playerUpgrade", "player");

            addIntSlider(upgradesMenu, "Health", "", (v) => { PlayerUtil.upgrade("health", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("health", avatar));
            addIntSlider(upgradesMenu, "Jump", "", (v) => { PlayerUtil.upgrade("jump", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("jump", avatar));
            addIntSlider(upgradesMenu, "Launch", "", (v) => { PlayerUtil.upgrade("launch", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("launch", avatar));
            addIntSlider(upgradesMenu, "Map Player Count", "", (v) => { PlayerUtil.upgrade("playercount", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("playercount", avatar));
            addIntSlider(upgradesMenu, "Range", "", (v) => { PlayerUtil.upgrade("range", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("range", avatar));
            addIntSlider(upgradesMenu, "Speed", "", (v) => { PlayerUtil.upgrade("speed", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("speed", avatar));
            addIntSlider(upgradesMenu, "Stamina", "", (v) => { PlayerUtil.upgrade("stamina", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("stamina", avatar));
            addIntSlider(upgradesMenu, "Strength", "", (v) => { PlayerUtil.upgrade("strength", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("strength", avatar));
            addIntSlider(upgradesMenu, "Throw", "", (v) => { PlayerUtil.upgrade("throw", avatar, v); }, 0, 9999, PlayerUtil.getUpgradeLevel("throw", avatar));

            openPage(upgradesMenu, "playerUpgrade");
        }

        private static void openSpawnMenu() {
            var spawnerMenu = createMenu("R.A.M. - Spawn", "spawn", "mainmenu");

            addButton(spawnerMenu, "Items", () => { navigate(spawnerMenu, "spawnItem"); });
            addButton(spawnerMenu, "Valuables", () => { navigate(spawnerMenu, "spawnValuable"); });
            addButton(spawnerMenu, "Enemies", () => { navigate(spawnerMenu, "spawnEnemy"); });

            openPage(spawnerMenu, "spawn");
        }

        private static void openSpawnItemsMenu() {
            var itemsMenu = createMenu("R.A.M. - Spawn - Items", "spawnItem", "spawn");

            foreach (KeyValuePair<string, Item> entry in ItemUtil.getItems()) {
                addButton(itemsMenu, entry.Key, () => ItemUtil.spawnItem(entry.Value));
            }

            openPage(itemsMenu, "spawnItem");
        }

        private static void openSpawnValuablesMenu() {
            var valuablesMenu = createMenu("R.A.M. - Spawn - Valuables", "spawnValuable", "spawn");


            addButton(valuablesMenu, "Tiny", () => { navigate(valuablesMenu, "spawnValuableTiny"); });
            addButton(valuablesMenu, "Small", () => { navigate(valuablesMenu, "spawnValuableSmall"); });
            addButton(valuablesMenu, "Medium", () => { navigate(valuablesMenu, "spawnValuableMedium"); });
            addButton(valuablesMenu, "Big", () => { navigate(valuablesMenu, "spawnValuableBig"); });
            addButton(valuablesMenu, "Wide", () => { navigate(valuablesMenu, "spawnValuableWide"); });
            addButton(valuablesMenu, "Tall", () => { navigate(valuablesMenu, "spawnValuableTall"); });
            addButton(valuablesMenu, "Very Tall", () => { navigate(valuablesMenu, "spawnValuableVeryTall"); });

            openPage(valuablesMenu, "spawnValuable");
        }

        private static void openSpawnValuablesTinyMenu() {
            var valuablesTinyMenu = createMenu("R.A.M. - Tiny Valuables", "spawnValuableTiny", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getTinyValuables()) {
                addButton(valuablesTinyMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesTinyMenu, "spawnValuableTiny");
        }

        private static void openSpawnValuablesSmallMenu() {
            var valuablesSmallMenu = createMenu("R.A.M. - Small Valuables", "spawnValuableSmall", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getSmallValuables()) {
                addButton(valuablesSmallMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesSmallMenu, "spawnValuableSmall");
        }

        private static void openSpawnValuablesMediumMenu() {
            var valuablesMediumMenu = createMenu("R.A.M. - Medium Valuables", "spawnValuableMedium", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getMediumValuables()) {
                addButton(valuablesMediumMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesMediumMenu, "spawnValuableMedium");
        }

        private static void openSpawnValuablesBigMenu() {
            var valuablesBigMenu = createMenu("R.A.M. - Big Valuables", "spawnValuableBig", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getBigValuables()) {
                addButton(valuablesBigMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesBigMenu, "spawnValuableBig");
        }

        private static void openSpawnValuablesWideMenu() {
            var valuablesWideMenu = createMenu("R.A.M. - Wide Valuables", "spawnValuableWide", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getWideValuables()) {
                addButton(valuablesWideMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesWideMenu, "spawnValuableWide");
        }

        private static void openSpawnValuablesTallMenu() {
            var valuablesTallMenu = createMenu("R.A.M. - Tall Valuables", "spawnValuableTall", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getTallValuables()) {
                addButton(valuablesTallMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesTallMenu, "spawnValuableTall");
        }

        private static void openSpawnValuablesVeryTallMenu() {
            var valuablesVeryTallMenu = createMenu("R.A.M. - Very Tall Valuables", "spawnValuableVeryTall", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getVeryTallValuables()) {
                addButton(valuablesVeryTallMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesVeryTallMenu, "spawnValuableVeryTall");
        }

        private static void openSpawnEnemyMenu() {
            var spawnerMenu = createMenu("R.A.M. - Spawn - Enemies", "spawnEnemy", "spawn");

            foreach(string enemy in EnemyUtil.getEnemies().Keys) {
                addButton(spawnerMenu, enemy, () => EnemyUtil.spawnEnemy(enemy));
            }

            foreach (string enemy in EnemyUtil.getModEnemies().Keys) {
                addButton(spawnerMenu, "(Mod) " + enemy, () => EnemyUtil.spawnEnemy(enemy));
            }

            openPage(spawnerMenu, "spawnEnemy");
        }

        private static void openMapMenu() {
            var mapMenu = createMenu("R.A.M. - Map Settings", "map", "mainmenu");

            foreach(KeyValuePair<string, Level> entry in MapUtil.getMaps()) {
                addToggle(mapMenu, entry.Key, (b) => { MapUtil.setMapEnabled(entry.Value, !b); }, "Off", "On", !MapUtil.isMapEnabled(entry.Value));
            }

            addButton(mapMenu, "Level Selector", () => { navigate(mapMenu, "levelSelector"); });
            addButton(mapMenu, "Discover Extraction Point", ExtractionPointUtil.discoverNext);
            addButton(mapMenu, "Complete Extraction Point", ExtractionPointUtil.complete);

            openPage(mapMenu, "map");
        }

        private static void openLevelSelectorMenu() {
            var mapMenu = createMenu("R.A.M. - Level Selector", "levelSelector", "map");

            foreach (KeyValuePair<string, Level> entry in MapUtil.getMaps()) {
                addButton(mapMenu, entry.Key, () => { MapUtil.changeLevel(entry.Value); });
            }
            addButton(mapMenu, "Arena", () => { MapUtil.changeLevel(RunManager.instance.levelArena); });
            addButton(mapMenu, "Lobby", () => { MapUtil.changeLevel(RunManager.instance.levelLobby); });
            addButton(mapMenu, "Shop", () => { MapUtil.changeLevel(RunManager.instance.levelShop); });

            openPage(mapMenu, "levelSelector");
        }

        private static void openSettingsMenu() {
            var settingsMenu = createMenu("R.A.M. - Settings", "settings", "mainmenu");

            addIntSlider(settingsMenu, "Level", "", (v) => { RunManager.instance.levelsCompleted = v; }, 1, 9999, RunManager.instance.levelsCompleted);
            addToggle(settingsMenu, "Infinite Money", (b) => { Settings.infiniteMoney = !b; }, "Off", "On", !Settings.infiniteMoney);
            addToggle(settingsMenu, "No Break", (b) => { Settings.noBreak = !b; }, "Off", "On", !Settings.noBreak);
            addToggle(settingsMenu, "No Battery/Ammo Drain", (b) => { Settings.noBatteryDrain = !b; }, "Off", "On", !Settings.noBatteryDrain);
            addToggle(settingsMenu, "No Traps", (b) => { Settings.noTraps = !b; }, "Off", "On", !Settings.noTraps);
            addToggle(settingsMenu, "Weak Enemies", (b) => { Settings.weakEnemies = !b; }, "Off", "On", !Settings.weakEnemies);
            addToggle(settingsMenu, "Deaf Enemies", (b) => { Settings.deafEnemies = !b; }, "Off", "On", !Settings.deafEnemies);
            addToggle(settingsMenu, "Blind Enemies", (b) => { Settings.blindEnemies = !b; }, "Off", "On", !Settings.blindEnemies);
            addToggle(settingsMenu, "Boom Hammer", (b) => { Settings.boomhammer = !b; }, "Off", "On", !Settings.boomhammer);
            addToggle(settingsMenu, "Friendly Duck", (b) => { Settings.friendlyDuck = !b; }, "Off", "On", !Settings.friendlyDuck);
            addToggle(settingsMenu, "Upgrade In Shop", (b) => { Settings.useShopUpgrades = !b; }, "Off", "On", !Settings.useShopUpgrades);

            openPage(settingsMenu, "settings");
        }

        private static void openCreditsMenu() {
            var creditsMenu = createMenu("R.A.M. - Credits", "credits", "mainmenu");

            addLabel(creditsMenu, "Repo Admin Menu by");
            addLabel(creditsMenu, "  proferabg  ");
            addLabel(creditsMenu, "");
            addLabel(creditsMenu, "Special thanks to:");
            addLabel(creditsMenu, " - REPOrium_Team");
            addLabel(creditsMenu, " - nickklmao");
            addLabel(creditsMenu, " - Godji");
            addLabel(creditsMenu, " - Zehs");
            addLabel(creditsMenu, "");
            addLabel(creditsMenu, "Repo Admin Menu © 2025");

            openPage(creditsMenu, "credits");
        }

        public static string getSelectedPlayer() {
            return selectedPlayerId;
        }
    }
}
