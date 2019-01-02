using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace KeyPresser
{

    public class KeyPressInfo : INotifyPropertyChanged
    {
        #region Properties
        [JsonProperty]
        public int IdKeyPressInfo { get; private set; }
        [JsonProperty]
        public bool IsActive { get { return this._IsActive; } set { this._IsActive = value; NotifyPropertyChanged(); } }
        [JsonProperty]
        public string KeyName { get; private set; }
        [JsonProperty]
        public Key Key { get; private set; }
        [JsonProperty]
        public int VirtualKey { get; private set; }
        [JsonProperty]
        public int Frequency { get; set; }//in miliseconds

        [JsonIgnore]
        public DateTime LastPressTime { get { return this._LastPressTime; } private set { this._LastPressTime = value; } }
        [JsonIgnore]
        public DateTime NextPressTime { get { return this._NextPressTime; } private set { this._NextPressTime = value; } }

        [JsonIgnore]
        public BitmapImage Remove { get { return _Remove; } }

        [JsonIgnore]
        public bool _IsActive;
        [JsonIgnore]
        private static BitmapImage _Remove;
        [JsonIgnore]
        private DateTime _LastPressTime;
        [JsonIgnore]
        private DateTime _NextPressTime;

        //HazardOptions
        [JsonIgnore]
        public bool HazardBehaviour { get; private set; }
        [JsonIgnore]
        private bool ResetHazardBehaviourOptionsOnNextPressCheck { get; set; }
        [JsonIgnore]
        public bool FakeClick { get; private set; }
        [JsonIgnore]
        public int FakeClickCount { get; private set; }
        [JsonIgnore]
        private DateTime LastFakeClickTime { get; set; }
        [JsonIgnore]
        public int DelayedClickCount { get; private set; }
        [JsonIgnore]
        public double PeriodPercentage { get; private set; }
        [JsonIgnore]
        private Random Randomizer { get; set; }

        //Statistics
        [JsonIgnore]
        public long PressCount { get; private set; }

        #endregion
        #region Consts
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint KEYEVENTF_KEYUP = 0x0002;
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        #region Ctor
        public KeyPressInfo(int IdKeyPressInfo, bool IsActive, string KeyName, Key Key, int Frequency = 1000, bool HazardBehaviour = false)
        {
            this.IdKeyPressInfo = IdKeyPressInfo;
            this.IsActive = IsActive;
            this.Frequency = Frequency;
            this.SetKey(Key, KeyName);

            this.ResetKeyPress();

            this.HazardBehaviour = HazardBehaviour;
            if (this.HazardBehaviour)
            {
                this.ResetHazardBehaviourOptions();
                this.Randomizer = new Random();
            }

            if (_Remove == null)
                _Remove = Utils.ToBitmapImage(ResourceImage48.Delete);
        }
        #endregion

        #region InitKeyPressTime
        public void InitKeyPressTime(DateTime? pressTime = null)
        {
            if (pressTime.HasValue)
                this.LastPressTime = pressTime.Value;
            else
                this.LastPressTime = DateTime.Now;
            this.NextPressTime = this.LastPressTime.AddMilliseconds(this.Frequency);
        }
        #endregion
        #region SetNextPressTime
        public bool SetNextPressTime()
        {
            try
            {
                DateTime referenceDate = DateTime.Now;
                if (this.LastPressTime == DateTime.MinValue)
                    this.InitKeyPressTime(referenceDate);
                if (this.HazardBehaviour)
                {
                    if (this.ResetHazardBehaviourOptionsOnNextPressCheck)
                        this.ResetHazardBehaviourOptions();
                    this.PeriodPercentage = ((TimeSpan)(referenceDate - this.LastPressTime)).TotalMilliseconds / this.Frequency;
                }
                if (this.NextPressTime < referenceDate)
                {
                    bool generateClick = true;
                    if (this.HazardBehaviour)
                    {
                        generateClick = this.GenerateFakeClick(referenceDate);
                        if (generateClick)
                        {
                            this.FakeClick = false;
                            this.ResetHazardBehaviourOptionsOnNextPressCheck = true;
                        }
                    }
                    if (generateClick)
                    {
                        this.LastPressTime = this.NextPressTime;
                        this.NextPressTime = this.NextPressTime.AddMilliseconds(this.Frequency);
                    }
                    return generateClick;
                }
                if (this.HazardBehaviour)
                {
                    bool generateClick = this.GenerateFakeClick(referenceDate);
                    if (!this.FakeClick && generateClick)
                        this.FakeClick = true;
                    return generateClick;
                }
                else
                    return false;
            }catch(Exception ex)
            {
                Logger.Log(EventID.KeyPressInfoException, this.IdKeyPressInfo, this.KeyName, ex);
                return false;
            }
        }
        #endregion
        #region SetKey
        public void SetKey(Key key, string keyName)
        {
            this.Key = key;
            this.KeyName = keyName;
            this.VirtualKey = KeyInterop.VirtualKeyFromKey(this.Key);
        }
        #endregion
        #region SetHazardBehaviour
        public void SetHazardBehaviour(bool hazardBehaviour)
        {
            if(hazardBehaviour != this.HazardBehaviour)
            {
                this.HazardBehaviour = hazardBehaviour;
                if(this.HazardBehaviour)
                {
                    if (this.Randomizer == null)
                        this.Randomizer = new Random();
                }
                else
                {
                    this.Randomizer = null;
                }
                this.ResetHazardBehaviourOptions();
            }
        }
        #endregion
        #region PressKey
        public void PressKey()
        {
            try
            {
                keybd_event((byte)this.VirtualKey, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
                keybd_event((byte)this.VirtualKey, 0, 2, 0);
                this.PressCount++;
            }
            catch(Exception ex)
            {
                Logger.Log(EventID.KeyPressInfoException, this.IdKeyPressInfo, this.KeyName, ex);
            }
        }
        #endregion
        #region GenerateFakeClick
        private bool GenerateFakeClick(DateTime? referenceDate = null)
        {
            try
            {
                if (!referenceDate.HasValue)
                    referenceDate = DateTime.Now;
                bool retValue = false;
                //Minimalny odstęp między kliknięciami 100ms
                if (((TimeSpan)(referenceDate - this.LastFakeClickTime)).TotalMilliseconds > 150)
                {
                    if (this.PeriodPercentage < 0.3)
                    {
                        //Duża sznasa na kolejne kliknięcie po poprzednim właściwym
                        //Zmniejsza się z każdym kolejnym kliknięciem
                        //Wejsciowo 5% czyli RAND % 20
                        retValue = (this.Randomizer.Next(101) % (20 * (this.FakeClickCount + 1))) == 0;
                    }
                    else if (this.PeriodPercentage < 0.9)
                    {
                        //Mała szansa na powtórne kliknięcie w środku okresu
                        //Zmniejsza się z każdym kolejnym kliknięciem
                        //Wejsciowo 3% czyli RAND % 30
                        retValue = (this.Randomizer.Next(101) % (30 * (this.FakeClickCount + 1))) == 0;
                    }
                    else if (this.PeriodPercentage < 1.0)
                    {
                        //Duża sznsa na kliknięcie - oczekiwanie na koniec okresu
                        //Zwiększa się wraz ze zmniejszaniem czasu do końca
                        //Wejsciowo 4% czyli RAND % 25
                        retValue = (this.Randomizer.Next(101) % (25 * (this.FakeClickCount + 1))) == 0;
                    }
                    else
                    {
                        retValue = !this.GenerateDelayedClick(referenceDate.Value);
                    }

                    if (retValue)
                    {
                        this.FakeClickCount++;
                        this.LastFakeClickTime = referenceDate.Value;
                    }
                }
                return retValue;
            }
            catch(Exception ex)
            {
                Logger.Log(EventID.KeyPressInfoException, this.IdKeyPressInfo, this.KeyName, ex);
                return false;
            }
        }
        #endregion
        #region GenerateDelayedClick
        /// <summary>
        /// Decyzja czy generujemy opóźnienie w kliknięciu
        /// true - generujemy opóźnienie (nie generujemy kliknięcia)
        /// false - nie generujemy opóźnienie (generujemy kliknięcie)
        /// </summary>
        /// <returns></returns>
        private bool GenerateDelayedClick(DateTime referenceDate)
        {
            bool retValue = false;
            try
            {
                //Prawdopodobieństwo opóźnionego kliknięcia zmniejsza się wraz z rosnącym opóźnieniem
                //Maksymalne opóźnienie 150% okresu
                if (this.PeriodPercentage >= 1.5)
                    return retValue;
                //Min 20%, Max 70% że wygenerowane zostanie opoznienie
                retValue = this.Randomizer.NextDouble() > Math.Max(0.3, (this.PeriodPercentage - 0.7));

                if (retValue)
                {
                    this.DelayedClickCount++;
                    this.LastFakeClickTime = referenceDate;
                }
            }catch(Exception ex)
            {
                Logger.Log(EventID.KeyPressInfoException, this.IdKeyPressInfo, this.KeyName, ex);
            }
            return retValue;
        }
        #endregion
        #region ResetHazardBehaviourOptions
        private void ResetHazardBehaviourOptions()
        {
            this.FakeClick = false;
            this.FakeClickCount = 0;
            this.DelayedClickCount = 0;
            this.PeriodPercentage = 0.0;
            this.LastFakeClickTime = DateTime.MinValue;
            this.ResetHazardBehaviourOptionsOnNextPressCheck = false;
        }
        #endregion
        #region ResetKeyPress
        public void ResetKeyPress()
        {
            this.LastPressTime = DateTime.MinValue;
            this.NextPressTime = DateTime.MinValue;
            this.PressCount = 0;
        }
        #endregion

        #region NotifyPropertyChanged
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region overload==
        public static bool operator ==(KeyPressInfo key1, KeyPressInfo key2)
        {
            if (ReferenceEquals(key1, key2))
                return true;
            if (ReferenceEquals(key1, null))
                return false;
            if (ReferenceEquals(key2, null))
                return false;
            return key1.IdKeyPressInfo == key2.IdKeyPressInfo;
        }
        #endregion
        #region overload!=
        public static bool operator !=(KeyPressInfo key1, KeyPressInfo key2)
        {
            return !(key1 == key2);
        }
        #endregion
        #region override Equals
        public override bool Equals(object obj)
        {
            if (obj is KeyPressInfo)
                return (obj as KeyPressInfo).IdKeyPressInfo == this.IdKeyPressInfo;
            return base.Equals(obj);
        }
        #endregion
        #region ovaerload GetHashCode
        public override int GetHashCode()
        {
            return this.IdKeyPressInfo.GetHashCode();
        }
        #endregion
    }
}
