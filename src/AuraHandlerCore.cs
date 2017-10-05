using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AuraHandler.FlaskComponents;
using Newtonsoft.Json;
using PoeHUD.Controllers;
using PoeHUD.Hud.Health;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.EntityComponents;
using SharpDX;
using System.Windows.Forms;
using Utility;
using GUI;

namespace AuraHandler
{
    internal class AuraHandlerCore : BaseSettingsPlugin<AuraHandlerSettings>
    {
        private const int LogmsgTime = 3;
        private const int ErrmsgTime = 10;
        private KeyboardHelper _keyboard;
        private Dictionary<string, float> _debugDebuff;

        private bool _isTown;
        private bool _isHideout;
        private bool _warnFlaskSpeed;
        private DebuffPanelConfig _debuffInfo;
        private FlaskInformation _flaskInfo;
        private FlaskKeys _keyInfo;
        private List<PlayerFlask> _playerFlaskList;

        private float _moveCounter;
        private float _lastManaUsed;
        private float _lastLifeUsed;
        private float _lastDefUsed;
        private float _lastOffUsed;

        #region AuraHandlerInit
        public void SplashPage()
        {
            ConfigFile startconfig = new ConfigFile();
            startconfig.Initialize();
            var SesamOpen = new MainWindow();
            if (!Settings.About.Value) return;
            SesamOpen.ShowDialog();
            Settings.About.Value = false;
        }
        public void BuffUi()
        {
            if (!Settings.BuffUiEnable.Value || _isTown) return;
            var x = GameController.Window.GetWindowRectangle().Width * Settings.BuffPositionX.Value * .01f;
            var y = GameController.Window.GetWindowRectangle().Height * Settings.BuffPositionY.Value * .01f;
            var position = new Vector2(x, y);
            float maxWidth = 0;
            float maxheight = 0;
            foreach (var buff in GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().Buffs)
            {
                var isInfinity = float.IsInfinity(buff.Timer);
                var isFlaskBuff = buff.Name.ToLower().Contains("flask");
                if (!Settings.EnableFlaskAuraBuff.Value && (isInfinity || isFlaskBuff))
                    continue;

                Color textColor;
                if (isFlaskBuff)
                    textColor = Color.SpringGreen;
                else if (isInfinity)
                    textColor = Color.Purple;
                else
                    textColor = Color.WhiteSmoke;

                var size = Graphics.DrawText(buff.Name + ":" + buff.Timer, Settings.BuffTextSize.Value, position, textColor);
                position.Y += size.Height;
                maxheight += size.Height;
                maxWidth = Math.Max(maxWidth, size.Width);
            }
            var background = new RectangleF(x, y, maxWidth, maxheight);
            Graphics.DrawFrame(background, 5, Color.Black);
            Graphics.DrawImage("lightBackground.png", background);
        }
        public void FlaskUi()
        {
            if (!Settings.FlaskUiEnable.Value) return;
            var x = GameController.Window.GetWindowRectangle().Width * Settings.FlaskPositionX.Value * .01f;
            var y = GameController.Window.GetWindowRectangle().Height * Settings.FlaskPositionY.Value * .01f;
            var position = new Vector2(x, y);
            float maxWidth = 0;
            float maxheight = 0;
            var textColor = Color.WhiteSmoke;

            foreach (var flasks in _playerFlaskList.ToArray())
            {
                if (!flasks.IsValid)
                    continue;
                if (!flasks.IsEnabled)
                    textColor = Color.Red;
                else switch (flasks.FlaskRarity)
                {
                    case ItemRarity.Magic:
                        textColor = Color.CornflowerBlue;
                        break;
                    case ItemRarity.Unique:
                        textColor = Color.Chocolate;
                        break;
                    case ItemRarity.Normal:
                        break;
                    case ItemRarity.Rare:
                        break;
                    default:
                        textColor = Color.WhiteSmoke;
                        break;
                }

                var size = Graphics.DrawText(flasks.FlaskName, Settings.FlaskTextSize.Value, position, textColor);
                position.Y += size.Height;
                maxheight += size.Height;
                maxWidth = Math.Max(maxWidth, size.Width);
            }
            var background = new RectangleF(x, y, maxWidth, maxheight);
            Graphics.DrawFrame(background, 5, Color.Black);
            Graphics.DrawImage("lightBackground.png", background);
        }
        public override void Render()
        {
            base.Render();
            if (!Settings.Enable.Value) return;
            FlaskUi();
         //   BuffUi();
        }
        public override void OnClose()
        {
            base.OnClose();
            if (!Settings.DebugMode.Value) return;
            foreach (var key in _debugDebuff)
            {
                File.AppendAllText(PluginDirectory + @"/debug.log", DateTime.Now + " " + key.Key + " : " + key.Value + Environment.NewLine);
            }
        }
        public override void Initialise()
        {
            PluginName = "PENIS";
            var bindFilename = PluginDirectory + @"/config/flaskbind.json";
            var flaskFilename = PluginDirectory + @"/config/flaskinfo.json";
            const string debufFilename = "config/debuffPanel.json";
            if (!File.Exists(bindFilename))
            {
                LogError("Cannot find " + bindFilename + " file. This plugin will exit.", ErrmsgTime);
                return;
            }
            if (!File.Exists(flaskFilename))
            {
                LogError("Cannot find " + flaskFilename + " file. This plugin will exit.", ErrmsgTime);
                return;
            }
            if (!File.Exists(debufFilename))
            {
                LogError("Cannot find " + debufFilename + " file. This plugin will exit.", ErrmsgTime);
                return;
            }
            var keyString = File.ReadAllText(bindFilename);
            var flaskString = File.ReadAllText(flaskFilename);
            var json = File.ReadAllText(debufFilename);
            _keyInfo = JsonConvert.DeserializeObject<FlaskKeys>(keyString);
            _debuffInfo = JsonConvert.DeserializeObject<DebuffPanelConfig>(json);
            _flaskInfo = JsonConvert.DeserializeObject<FlaskInformation>(flaskString);
            _playerFlaskList = new List<PlayerFlask>(5);
            _debugDebuff = new Dictionary<string, float>();
            OnAuraHandlerToggle();
            Settings.Enable.OnValueChanged += OnAuraHandlerToggle;
        }
        private void OnAreaChange(AreaController area)
        {
            if (Settings.Enable.Value)
            {
                LogMessage("Area has been changed. Loading flasks info.", LogmsgTime);
                _isHideout = area.CurrentArea.IsHideout;
                _isTown = area.CurrentArea.IsTown;
                foreach (var flask in _playerFlaskList)
                {
                    flask.TotalTimeUsed = (flask.IsInstant) ? 100000 : 0;
                }
            }
        }
        private void OnAuraHandlerToggle()
        {
            try
            {
                if (Settings.Enable.Value)
                {
                    if (Settings.DebugMode.Value)
                        LogMessage("Enabling AuraHandler.", LogmsgTime);
                    GameController.Area.OnAreaChange += OnAreaChange;
                    _moveCounter = 0f;
                    _lastManaUsed = 100000f;
                    _lastLifeUsed = 100000f;
                    _lastDefUsed = 100000f;
                    _lastOffUsed = 100000f;
                    _isTown = true;
                    _isHideout = false;
                    _warnFlaskSpeed = true;
                    _keyboard = new KeyboardHelper(GameController);
                    _playerFlaskList.Clear();
                    for (var i = 0; i < 5; i++)
                        _playerFlaskList.Add(new PlayerFlask(i));

                    //We are creating our plugin thread inside PoEHUD!
                    var flaskThread = new Thread(FlaskThread) { IsBackground = true };
                    flaskThread.ApartmentState = ApartmentState.STA;
                    flaskThread.Start();
                }
                else
                {
                    if (Settings.DebugMode.Value)
                        LogMessage("Disabling AuraHandler.", LogmsgTime);
                    GameController.Area.OnAreaChange -= OnAreaChange;
                    _playerFlaskList.Clear();
                }
            }
            catch (Exception)
            {

                LogError("Error Starting AuraHandler Thread.", ErrmsgTime);
            }
        }
        private void FlaskThread()
        {
            while (Settings.Enable.Value)
            {
                FlaskMain();
                SplashPage();
                Thread.Sleep(100);
            }
        }
        #endregion
        #region GettingFlaskDetails
        private bool GettingAllFlaskInfo()
        {
            try
            {
                for (var j = 0; j < 5; j++)
                {
                    //InventoryItemIcon flask = flasksEquipped[j].AsObject<InventoryItemIcon>();
                    var flaskItem = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.Flask][j, 0, 0];
                    if (flaskItem == null || flaskItem.Address == 0)
                    {
                        _playerFlaskList[j].IsValid = false;
                        continue;
                    }

                    var flaskCharges = flaskItem.GetComponent<Charges>();
                    var flaskMods = flaskItem.GetComponent<Mods>();
                    _playerFlaskList[j].IsInstant = false;
                    float tmpUseCharges = flaskCharges.ChargesPerUse;
                    _playerFlaskList[j].CurrentCharges = flaskCharges.NumCharges;
                    _playerFlaskList[j].FlaskRarity = flaskMods.ItemRarity;
                    _playerFlaskList[j].FlaskName = GameController.Files.BaseItemTypes.Translate(flaskItem.Path).BaseName;
                    _playerFlaskList[j].FlaskAction2 = FlaskActions.None;
                    _playerFlaskList[j].FlaskAction1 = FlaskActions.None;

                    //Checking flask action based on flask name type.
                    if (!_flaskInfo.FlaskTypes.TryGetValue(_playerFlaskList[j].FlaskName, out _playerFlaskList[j].FlaskAction1))
                        LogError("Error: " + _playerFlaskList[j].FlaskName + " name not found. Report this error message.", ErrmsgTime);

                    if (Settings.ChargeReduction.Value > 0)
                        tmpUseCharges = ((100 - Settings.ChargeReduction.Value) / 100) * tmpUseCharges;

                    //Checking flask mods.
                    FlaskActions action2 = FlaskActions.Ignore;
                    foreach (var mod in flaskMods.ItemMods)
                    {
                        if (mod.Name.ToLower().Contains("flaskchargesused"))
                            tmpUseCharges = ((100 + (float)mod.Value1) / 100) * tmpUseCharges;

                        if (mod.Name.ToLower().Contains("instant"))
                            _playerFlaskList[j].IsInstant = true;

                        // We have already decided action2 for unique flasks.
                        if (flaskMods.ItemRarity == ItemRarity.Unique)
                            continue;

                        if (!_flaskInfo.FlaskMods.TryGetValue(mod.Name, out action2))
                            LogError("Error: " + mod.Name + " mod not found. Is it unique flask? If not, report this error message.", ErrmsgTime);
                        else if (action2 != FlaskActions.Ignore)
                            _playerFlaskList[j].FlaskAction2 = action2;
                    }

                    // Speedrun mod on mana/life flask wouldn't work when full mana/life is full respectively,
                    // So we will ignore speedrun mod from mana/life flask. Other mods
                    // on mana/life flasks will work.
                    if (_playerFlaskList[j].FlaskAction2 == FlaskActions.Speedrun &&
                        (_playerFlaskList[j].FlaskAction1 == FlaskActions.Life ||
                         _playerFlaskList[j].FlaskAction1 == FlaskActions.Mana ||
                         _playerFlaskList[j].FlaskAction1 == FlaskActions.Hybrid))
                    {
                        _playerFlaskList[j].FlaskAction2 = FlaskActions.None;
                        if (_warnFlaskSpeed)
                        {
                            LogError("Warning: Speed Run mod is ignored on mana/life/hybrid flasks. Use Alt Orbs on those flasks.", ErrmsgTime);
                            _warnFlaskSpeed = false;
                        }
                    }

                    if (Settings.DisableLifeSecUse.Value)
                    {
                        if (_playerFlaskList[j].FlaskAction1 == FlaskActions.Life || _playerFlaskList[j].FlaskAction1 == FlaskActions.Hybrid)
                            if (_playerFlaskList[j].FlaskAction2 == FlaskActions.Offense || _playerFlaskList[j].FlaskAction2 == FlaskActions.Defense)
                                _playerFlaskList[j].FlaskAction2 = FlaskActions.None;
                    }

                    _playerFlaskList[j].UseCharges = (int)Math.Floor(tmpUseCharges);
                    _playerFlaskList[j].IsValid = true;
                }
            }
            catch (Exception e)
            {
                if (Settings.DebugMode.Value)
                {
                    LogError("Warning: Error getting all flask Informations.", ErrmsgTime);
                    LogError(e.Message + e.StackTrace, ErrmsgTime);
                }
                _playerFlaskList.Clear();
                for (var i = 0; i < 5; i++)
                    _playerFlaskList.Add(new PlayerFlask(i));
                return false;
            }
            return true;
        }
        #endregion
        #region Flask Helper Functions
        public void UpdateFlaskChargesInfo(int slot)
        {
            try
            {
                _playerFlaskList[slot].CurrentCharges = GameController.Game.IngameState.
                    IngameUi.InventoryPanel[InventoryIndex.Flask][slot, 0, 0].
                    GetComponent<Charges>().NumCharges;
            }
            catch (Exception)
            {
                _playerFlaskList[slot].CurrentCharges = 0;
            }
        }
        //Parameters:
        // type1 and type2 define the type of flask you wanna drink.
        // reason: is just for debugging output to see where does the drinking flask request came from
        // minRequiredCharges: Min number of charges a flask must have to consider it a valid flask to drink.
        // shouldDrinkAll: if you want to drink all the flasks of type1,type2 (true) or just first in the list(false).
        private bool FindDrinkFlask(FlaskActions type1, FlaskActions type2, string reason, int minRequiredCharge = 0, bool shouldDrinkAll = false)
        {
            var hasDrunk = false;
            var flaskList = _playerFlaskList.FindAll(x => (x.FlaskAction1 == type1 || x.FlaskAction2 == type2 ||
            x.FlaskAction1 == type2 || x.FlaskAction2 == type1) && x.IsEnabled && x.IsValid);

            flaskList.Sort( (x,y) => x.TotalTimeUsed.CompareTo(y.TotalTimeUsed));
            foreach (var flask in flaskList)
            {
                UpdateFlaskChargesInfo(flask.Slot);
                if (flask.CurrentCharges < flask.UseCharges || flask.CurrentCharges < minRequiredCharge) continue;
                _keyboard.SetLatency(GameController.Game.IngameState.CurLatency);
                if (!_keyboard.KeyPressRelease(_keyInfo.K[flask.Slot]))
                    LogError("Warning: High latency ( more than 1000 millisecond ), plugin will fail to work properly.", ErrmsgTime);
                if (Settings.DebugMode.Value)
                    LogMessage("Just Drank Flask on key " + _keyInfo.K[flask.Slot] + " cuz of " + reason, LogmsgTime);
                flask.TotalTimeUsed = (flask.IsInstant) ? 100000 : flask.TotalTimeUsed + 1;
                // if there are multiple flasks, drinking 1 of them at a time is enough.
                hasDrunk = true;
                if (!shouldDrinkAll)
                    return true;
            }
            return hasDrunk;
        }
        private static bool HasDebuff(IReadOnlyDictionary<string, int> dictionary, string buffName, bool isHostile)
        {
            int filterId = 0;
            if (dictionary.TryGetValue(buffName, out filterId))
            {
                return filterId == 0 || isHostile == (filterId == 1);
            }
            return false;
        }
        #endregion

        #region Auto Health Flasks
        private bool InstantLifeFlask(Life playerHealth)
        {
            if (playerHealth.HPPercentage * 100 < Settings.InstantPerHpFlask.Value)
            {
                var flaskList = _playerFlaskList.FindAll(x => x.IsInstant == x.IsEnabled == x.IsValid &&
                          (x.FlaskAction1 == FlaskActions.Life || x.FlaskAction1 == FlaskActions.Hybrid));
                foreach (var flask in flaskList)
                {
                    if (flask.CurrentCharges >= flask.UseCharges)
                    {
                        _keyboard.SetLatency(GameController.Game.IngameState.CurLatency);
                        if (!_keyboard.KeyPressRelease(_keyInfo.K[flask.Slot]))
                            LogError("Warning: High latency ( more than 1000 millisecond ), plugin will fail to work properly.", ErrmsgTime);
                        UpdateFlaskChargesInfo(flask.Slot);
                        if (Settings.DebugMode.Value)
                            LogMessage("Just Drank Instant Flask on key " + _keyInfo.K[flask.Slot] + " cuz of Very Low Life", LogmsgTime);
                        return true;
                    }
                    else
                    {
                        UpdateFlaskChargesInfo(flask.Slot);
                    }
                }
            }
            return false;
        }
        private void LifeLogic()
        {
            if (!GameController.Game.IngameState.Data.LocalPlayer.IsValid || !Settings.AutoFlask.Value)
                return;

            var localPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var playerHealth = localPlayer.GetComponent<Life>();
            _lastLifeUsed += 100f;
            if (InstantLifeFlask(playerHealth))
                return;
            if (playerHealth.HasBuff("flask_effect_life"))
                return;
            if (playerHealth.HPPercentage * 100 < Settings.PerHpFlask.Value)
            {
                if (FindDrinkFlask(FlaskActions.Life, FlaskActions.Ignore, "Low life"))
                    _lastLifeUsed = 0f;
                else if (FindDrinkFlask(FlaskActions.Hybrid, FlaskActions.Ignore, "Low life"))
                    _lastLifeUsed = 0f;
            }
        }
        #endregion
        #region Auto Mana Flasks
        private bool InstantManaFlask(Life playerHealth)
        {
            if (playerHealth.MPPercentage * 100 < Settings.InstantPerMpFlask.Value)
            {
                var flaskList = _playerFlaskList.FindAll(x => x.IsInstant == x.IsEnabled == x.IsValid &&
                          (x.FlaskAction1 == FlaskActions.Mana || x.FlaskAction1 == FlaskActions.Hybrid));
                foreach (var flask in flaskList)
                {
                    if (flask.CurrentCharges >= flask.UseCharges)
                    {
                        _keyboard.SetLatency(GameController.Game.IngameState.CurLatency);
                        if (!_keyboard.KeyPressRelease(_keyInfo.K[flask.Slot]))
                            LogError("Warning: High latency ( more than 1000 millisecond ), plugin will fail to work properly.", ErrmsgTime);
                        UpdateFlaskChargesInfo(flask.Slot);
                        if (Settings.DebugMode.Value)
                            LogMessage("Just Drank Instant Flask on key " + _keyInfo.K[flask.Slot] + " cuz of Very Low Mana", LogmsgTime);
                        return true;
                    }
                    else
                    {
                        UpdateFlaskChargesInfo(flask.Slot);
                    }
                }
            }
            return false;
        }
        private void ManaLogic()
        {
            if (!Settings.AutoFlask.Value || !GameController.Game.IngameState.Data.LocalPlayer.IsValid)
                return;

            var localPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var playerHealth = localPlayer.GetComponent<Life>();
            _lastManaUsed += 100f;
            if (InstantManaFlask(playerHealth))
                return;
            if (playerHealth.HasBuff("flask_effect_mana"))
                return;
            if (playerHealth.MPPercentage * 100 < Settings.PerManaFlask.Value || playerHealth.CurMana < Settings.MinManaFlask.Value)
            {
                if (FindDrinkFlask(FlaskActions.Mana, FlaskActions.Ignore, "Low Mana"))
                    _lastManaUsed = 0f;
                else if (FindDrinkFlask(FlaskActions.Hybrid, FlaskActions.Ignore, "Low Mana"))
                    _lastManaUsed = 0f;
            }
        }
        #endregion
        
        #region Auto Speed Flasks
        private void SpeedFlaskLogic()
        {
            if (!Settings.SpeedFlaskEnable.Value)
                return;

            var localPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var playerHealth = localPlayer.GetComponent<Life>();
            var playerMovement = localPlayer.GetComponent<Actor>();
            _moveCounter = playerMovement.isMoving ? _moveCounter += 100f : 0;
            var hasDrunkQuickSilver = false;

            if (localPlayer.IsValid && Settings.QuicksilverEnable.Value && _moveCounter >= Settings.QuicksilverDurration.Value &&
                !playerHealth.HasBuff("flask_bonus_movement_speed") &&
                !playerHealth.HasBuff("flask_utility_sprint"))
            {
                hasDrunkQuickSilver = FindDrinkFlask(FlaskActions.Speedrun, FlaskActions.Speedrun, "Moving Around", Settings.QuicksilverUseWhenCharges.Value);
            }

            // Given preference to QuickSilver cuz it give +40 and Silver give +20
            if (!Settings.ShouldDrinkSilverQuickSilverTogether.Value &&
                (hasDrunkQuickSilver || playerHealth.HasBuff("flask_bonus_movement_speed")
                || playerHealth.HasBuff("flask_utility_sprint")))
                return;

            if (localPlayer.IsValid && Settings.SilverFlaskEnable.Value && _moveCounter >= Settings.SilverFlaskDurration.Value &&
                !playerHealth.HasBuff("flask_utility_haste"))
            {
                FindDrinkFlask(FlaskActions.OFFENSE_AND_SPEEDRUN, FlaskActions.OFFENSE_AND_SPEEDRUN, "Moving Around", Settings.SilverFlaskUseWhenCharges.Value);
            }
        }
        #endregion

        private void FlaskMain()
        {
            if (!GameController.Window.IsForeground())
                return;

            if (!GameController.Game.IngameState.Data.LocalPlayer.IsValid)
                return;

            _playerFlaskList[0].IsEnabled = Settings.FlaskSlot1Enable.Value;
            _playerFlaskList[1].IsEnabled = Settings.FlaskSlot2Enable.Value;
            _playerFlaskList[2].IsEnabled = Settings.FlaskSlot3Enable.Value;
            _playerFlaskList[3].IsEnabled = Settings.FlaskSlot4Enable.Value;
            _playerFlaskList[4].IsEnabled = Settings.FlaskSlot5Enable.Value;
            if (!GettingAllFlaskInfo())
            {
                if (Settings.DebugMode.Value)
                    LogMessage("Error getting Flask Details, trying again.", LogmsgTime);
                return;
            }

            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            if (playerLife == null || _isTown || _isHideout || playerLife.CurHP <= 0 || playerLife.HasBuff("grace_period"))
                return;

            SpeedFlaskLogic();
            ManaLogic();
            LifeLogic();
        }
    }
}
