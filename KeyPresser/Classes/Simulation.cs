using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KeyPresser
{
    public class Simulation
    {
        #region Properties

        private ObservableCollection<KeyPressInfo> Keys { get; set; }

        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        public bool IsRunning { get { return this.StartTime.HasValue && !this.EndTime.HasValue; } }
        public bool IsHazardBehaviour { get; private set; }

        #endregion
        #region Members

        private readonly ManualResetEvent StopEvent = new ManualResetEvent(false);

        public delegate void AddLog(string log, bool threadSafeRequired = true);
        public delegate void KeyPressedEvent(object sender, KeyPressEventArgs e);
        public delegate void KeyChanged(object sender, KeyCollectionItemChangedEventArgs e);

        private Task KeyPressTask = null;

        public event KeyPressedEvent Key_Pressed;
        public event KeyChanged Key_Changed;

        public AddLog Log = null;

        #endregion

        #region Ctor
        public Simulation()
        {
            this.Keys = new ObservableCollection<KeyPressInfo>();
            this.SetHazardBehaviour(false);
            this.Reset();
            this.AddEmpyNewRow();
        }

        #endregion

        #region Events
        
        private void Key_CollectionItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if(Key_Changed!= null)
                this.Key_Changed(sender, new KeyCollectionItemChangedEventArgs(sender as KeyPressInfo, e.PropertyName));
        }

        #endregion

        #region Start
        public void Start()
        {
            try
            {
                if (!this.IsRunning)
                {
                    this.StartTime = DateTime.Now;
                    this.EndTime = null;
                    if (this.Keys != null)
                    {
                        foreach (KeyPressInfo kpiItem in this.Keys)
                        {
                            if (kpiItem.IdKeyPressInfo == 0)
                                continue;
                            kpiItem.ResetKeyPress();
                        }
                        this.StopEvent.Reset();
                        this.KeyPressTask = new Task(() => { SimulationProcess(); }, TaskCreationOptions.LongRunning);
                        this.KeyPressTask.Start();
                    }
                    Logger.Log(EventID.SimulationStart, this.IsHazardBehaviour);
                    if(this.Keys != null)
                    {
                        foreach (KeyPressInfo kpiItem in this.Keys)
                            Logger.Log(EventID.SimulationKey, kpiItem.KeyName, (int)kpiItem.Key, kpiItem.Frequency);
                    }
                }
            }catch(Exception ex)
            {
                Logger.Log(EventID.SimulationException, ex);
            }
        }
        #endregion
        #region Stop
        public void Stop()
        {
            long? clickCount = null;
            TimeSpan? simulatonTime = null;
            try
            {
                if (this.IsRunning)
                {
                    this.EndTime = DateTime.Now;
                    this.StopEvent.Set();
                    if (this.StartTime.HasValue && this.EndTime.HasValue)
                        simulatonTime = this.EndTime.Value - this.StartTime.Value;
                    clickCount = this.GetKeyPressCount();
                    try
                    {
                        if (!Task.WaitAll(new Task[] { this.KeyPressTask }, TimeSpan.FromSeconds(5)))
                            this.KeyPressTask.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (this.Log != null)
                            this.Log("Error during cancelling press task");
                        Logger.Log(EventID.SimulationException, ex);
                    }
                    finally
                    {
                        if (this.KeyPressTask != null)
                        {
                            try
                            {
                                this.KeyPressTask.Dispose();
                                this.KeyPressTask = null;
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(EventID.SimulationException, ex);
                                if (this.Log != null)
                                    this.Log("Critical error during cancelling press task");
                            }
                        }
                    }
                }
            }catch(Exception ex)
            {
                Logger.Log(EventID.SimulationException, ex);
            }finally
            {
                Logger.Log(EventID.SimulationEnd, 
                    simulatonTime.HasValue ? simulatonTime.Value.ToString(@"hh\:mm\:ss") : "<null>", 
                    clickCount.HasValue ? clickCount.Value.ToString() : "<null>");
            }
        }
        #endregion
        #region Reset
        public void Reset()
        {
            if (!this.IsRunning)
            {
                this.StartTime = null;
                this.EndTime = null;
            }
        }
        #endregion

        #region GetKeyPressInfo
        public ObservableCollection<KeyPressInfo> GetKeyPressInfo()
        {
            return this.Keys;
        }
        #endregion
        #region AddOrUpdateKeyPressInfo
        public void AddOrUpdateKeyPressInfo(KeyPressInfo kpiItem, Tuple<string, Key> pressedKey)
        {
            if (!this.IsRunning)
            {
                string keyName = null;
                Key key = Key.None;
                if (pressedKey != null)
                {
                    keyName = pressedKey.Item1;
                    key = pressedKey.Item2;
                }
                if (!String.IsNullOrWhiteSpace(keyName) && key != Key.None)
                {
                    if (kpiItem.IdKeyPressInfo == 0)
                    {
                        AddNewKeyPressInfo(true, keyName, key);
                    }
                    else
                    {
                        kpiItem.SetKey(key, keyName);
                    }
                }
            }
        }
        #endregion
        #region AddNewKeyPressInfo
        public void AddNewKeyPressInfo(bool IsActive, string KeyName, Key Key, int Frequency = 1000)
        {
            if (!this.IsRunning)
            {
                int nextIdKeyPressInfo = this.Keys.Count == 0 ? 1 : this.Keys.Max(k => k.IdKeyPressInfo) + 1;
                KeyPressInfo kpiItem = new KeyPressInfo(nextIdKeyPressInfo, IsActive, KeyName, Key, Frequency: Frequency, HazardBehaviour: this.IsHazardBehaviour);
                if (this.Keys.Count > 0 && this.Keys.Last().IdKeyPressInfo == 0)
                    this.Keys.Insert(this.Keys.Count - 1, kpiItem);
                else
                    this.Keys.Add(kpiItem);
                kpiItem.PropertyChanged += Key_CollectionItemChanged;
                Logger.Log(EventID.SimulationAddedKey, KeyName, (int)Key, IsActive, Frequency);
            }
        }
        #endregion
        #region AddEmpyNewRow
        private void AddEmpyNewRow()
        {
            if (!this.Keys.Any(k => k.IdKeyPressInfo == 0))
            {
                //At the End new empty row
                this.Keys.Add(new KeyPressInfo(0, false, "Kliknij aby dodać", Key.None));
            }
        }
        #endregion
        #region LoadKeyPressInfo
        public void LoadKeyPressInfo(ObservableCollection<KeyPressInfo> keys)
        {
            if (!this.IsRunning)
            {
                if (keys == null)
                    this.Keys = new ObservableCollection<KeyPressInfo>();
                else
                {
                    foreach (KeyPressInfo kpiItem in this.Keys)
                        kpiItem.PropertyChanged -= Key_CollectionItemChanged;
                    this.Keys = keys;
                    foreach (KeyPressInfo kpiItem in this.Keys)
                        kpiItem.PropertyChanged += Key_CollectionItemChanged;
                }
                this.AddEmpyNewRow();
            }
        }
        #endregion
        #region RemoveKeyPressInfo
        public bool RemoveKeyPressInfo(KeyPressInfo toDelete)
        {
            if (this.IsRunning)
                return false;
            if (toDelete.IdKeyPressInfo > 0)
            {
                toDelete.PropertyChanged -= Key_CollectionItemChanged;
                this.Keys.Remove(toDelete);
                return true;
            }
            return false;
        }
        #endregion

        #region HasKeys

        public bool HasKeys()
        {
            if (this.Keys != null)
            {
                return this.Keys.Any(k => k.IdKeyPressInfo != 0);
            }
            return false;
        }

        #endregion
        #region HasActiveKeys

        public bool HasActiveKeys()
        {
            if(this.Keys != null)
            {
                return this.Keys.Any(k => k.IdKeyPressInfo != 0 && k.IsActive);
            }
            return false;
        }

        #endregion

        #region GetClickCount

        public long GetKeyPressCount()
        {
            long retValue = 0;
            if (this.Keys != null)
                retValue = this.Keys.Sum(k => k.PressCount);
            return retValue;
        }

        #endregion

        #region SetHazardBehaviour
        public void SetHazardBehaviour(bool isHazardBehaviour)
        {
            if (!this.IsRunning)
            {
                this.IsHazardBehaviour = isHazardBehaviour;
                foreach (KeyPressInfo kpiItem in this.Keys)
                    kpiItem.SetHazardBehaviour(this.IsHazardBehaviour);
            }
        }
        #endregion

        #region SimulationProcess
        private void SimulationProcess()
        {
            Logger.Log(EventID.SimulationTaskStart);
            while (!StopEvent.WaitOne(50, false))
            {
                try
                {
                    if (StopEvent.WaitOne(0, false))
                        break;
                    if (this.Keys != null)
                    {
                        foreach (KeyPressInfo kpiItem in this.Keys.Where(k => k.IsActive))
                        {
                            if (kpiItem.IdKeyPressInfo == 0)
                                continue;
                            if (kpiItem.SetNextPressTime())
                            {
                                kpiItem.PressKey();
                                if (this.Key_Pressed != null)
                                    this.Key_Pressed(this, new KeyPressEventArgs(kpiItem));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(EventID.SimulationException, ex);
                    if (this.Log != null)
                        Log("Error during sim loop");
                }
            }
            Logger.Log(EventID.SimulationTaskEnd);

        }
        #endregion
    }

}
