using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Newtonsoft.Json;
using Syncfusion.SfSkinManager;

namespace KeyPresser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties

        public Simulation Simulation { get; set; }
        private KeyCatchInfo KeyCatchInfo { get; set; }

        #endregion
        #region Consts

        const string ColumnKeyName = "KeyName";
        const string ColumnRemove = "Remove";
        const string ColumnIsActive = "IsActive";

        const string UserSettingsDirectory = "UserSettings";
        const string UserSettingsFile = "UserSettings.clk";

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            try
            {
                Logger.Initialize();
                Logger.Log(EventID.KeyPresserStart);

                #region GlobalUnhandledExceptionEvents

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    UnhandledException_Raised((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

                Application.Current.DispatcherUnhandledException += (s, e) =>
                    UnhandledException_Raised(e.Exception, "Application.Current.DispatcherUnhandledException");

                TaskScheduler.UnobservedTaskException += (s, e) =>
                    UnhandledException_Raised(e.Exception, "TaskScheduler.UnobservedTaskException");

                #endregion

                SfSkinManager.ApplyStylesOnApplication = true;
                SfSkinManager.SetVisualStyle(this, Syncfusion.SfSkinManager.VisualStyles.Office2013DarkGray);

                #region KeyCatchInfo

                this.KeyCatchInfo = new KeyCatchInfo();
                this.KeyCatchInfo.Install();
                this.KeyCatchInfo.KeyUp += KeyCatchInfo_KeyUp;

                #endregion

                #region SimulationConf
                this.Simulation = new Simulation();
                this.Simulation.Log = this.Log;
                this.Simulation.Key_Pressed += Key_Pressed;
                this.Simulation.Key_Changed += Key_CollectionChanged;
                #region dgKeyPressConf
                this.LoadUserSettings();

                if (!this.Simulation.HasKeys())
                {
                    AddNewKeyPressInfo(true, "1", Key.D1);
                    AddNewKeyPressInfo(true, "2", Key.D2);
                    AddNewKeyPressInfo(true, "3", Key.D3);
                    AddNewKeyPressInfo(true, "4", Key.D4);
                }


                this.dgKeyPressConf.ItemsSource = this.Simulation.GetKeyPressInfo();
                #endregion
                SetSimulationButtonEditableProperty(this.Simulation.IsRunning);
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }


        #region Events

        #region dgKeyPressConf_CellTapped
        private void dgKeyPressConf_CellTapped(object sender, Syncfusion.UI.Xaml.Grid.GridCellTappedEventArgs e)
        {
            if (e.Column != null && e.Record != null && e.Record is KeyPressInfo)
            {
                KeyPressInfo kpiClickedItem = e.Record as KeyPressInfo;
                if (String.Equals(ColumnKeyName, e.Column.MappingName))
                {
                    this.AddOrUpdateKeyPressInfo(kpiClickedItem, this.GetPressedKey());
                }
                else if (String.Equals(ColumnRemove, e.Column.MappingName))
                {
                    this.RemoveKeyPressInfo(kpiClickedItem);
                }
            }
        }
        #endregion

        #region baSimulation_Click
        private void baSimulation_Click(object sender, RoutedEventArgs e)
        {
            this.ToogleSimulation();
        }
        #endregion

        #region Window_Closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try { if (this.Simulation != null && this.Simulation.IsRunning) this.Simulation.Stop(); } catch(Exception ex) { Console.WriteLine(ex.ToString()); Logger.Log(EventID.KeyPresserException, ex); }
            try { if (this.KeyCatchInfo != null) this.KeyCatchInfo.Uninstall(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); Logger.Log(EventID.KeyPresserException, ex); }
            try { SaveUserSettings(); }catch(Exception ex) { Console.WriteLine(ex.ToString()); Logger.Log(EventID.KeyPresserException, ex); }
            Logger.Log(EventID.KeyPresserEnd);
            try { Logger.Close(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); Logger.Log(EventID.KeyPresserException, ex); }
        }
        #endregion

        #region Key_Pressed
        private void Key_Pressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyPressed != null)
            {
                Log(String.Format("{0} - {1}({2}C,FC_{3},DC_{4},%_{5})",
                                            DateTime.Now.ToString("HH:mm:ss.fff"),
                                            e.KeyPressed.KeyName,
                                            e.KeyPressed.FakeClick ? "F" : "R",
                                            e.KeyPressed.FakeClickCount,
                                            e.KeyPressed.DelayedClickCount,
                                            e.KeyPressed.PeriodPercentage));
                tbKeyPressCount.Dispatcher.Invoke(new Action(() => { tbKeyPressCount.Text = this.Simulation.GetKeyPressCount().ToString(); }));
            }
        }
        #endregion

        #region Key_CollectionChanged
        private void Key_CollectionChanged(object sender, KeyCollectionItemChangedEventArgs e)
        {
            if (this.Simulation != null)
                this.SetSimulationButtonEditableProperty(this.Simulation.IsRunning);
        }
        #endregion

        #region KeyCatchInfo_KeyUp

        private void KeyCatchInfo_KeyUp(Key key)
        {
            if(key == Key.F4 || key == Key.F5 || key == Key.F6 || key == Key.F7 || key == Key.F8 || key == Key.F9)
            {
                ToogleSimulation();
            }
        }

        #endregion

        #region UnhandledException_Raised

        private void UnhandledException_Raised(Exception exception, string source)
        {
            string message = null;
            try
            {
                message = String.Format("Unhandled exception in {0}, exception: {1}.", source, exception?.ToString());
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message += String.Format("Exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                Logger.Log(EventID.KeyPresserUnhandledExceptionException, ex);
            }
            finally
            {
                Logger.Log(EventID.KeyPresserUnhandledException, message);
            }
        }

        #endregion

        #endregion

        #region UserSettings

        #region LoadUserSettings

        private void LoadUserSettings()
        {
            try
            {
                if(this.Simulation != null && Directory.Exists(UserSettingsDirectory))
                {
                    string userSettings = File.ReadAllText(Path.Combine(UserSettingsDirectory, UserSettingsFile));
                    if(!String.IsNullOrWhiteSpace(userSettings))
                    {
                        this.Simulation.LoadKeyPressInfo(JsonConvert.DeserializeObject<ObservableCollection<KeyPressInfo>>(userSettings));
                    }
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }

        #endregion
        #region SaveUserSettings

        private void SaveUserSettings()
        {
            try
            {
                if (this.Simulation != null)
                {
                    if (!Directory.Exists(UserSettingsDirectory))
                        Directory.CreateDirectory(UserSettingsDirectory);
                    string userSettings = String.Empty;
                    ObservableCollection<KeyPressInfo> keys = this.Simulation.GetKeyPressInfo();
                    if (keys != null && keys.Count > 0)
                        userSettings = JsonConvert.SerializeObject(keys);
                    File.WriteAllText(Path.Combine(UserSettingsDirectory, UserSettingsFile), userSettings);
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }

        #endregion

        #endregion

        #region Simulation

        #region AddOrUpdateKeyPressInfo
        private void AddOrUpdateKeyPressInfo(KeyPressInfo kpiItem, Tuple<string, Key> pressedKey)
        {
            if (this.Simulation != null)
            {
                try
                {
                    this.Simulation.AddOrUpdateKeyPressInfo(kpiItem, pressedKey);
                    this.dgKeyPressConf.View.Refresh();
                    SetSimulationButtonEditableProperty(this.Simulation.IsRunning);
                }
                catch(Exception ex)
                {
                    Logger.Log(EventID.KeyPresserException, ex);
                }
            }
        }
        #endregion
        #region AddNewKeyPressInfo
        private void AddNewKeyPressInfo(bool IsActive, string KeyName, Key Key, int Frequency = 1000)
        {
            if (this.Simulation != null)
            {
                try
                {
                    this.Simulation.AddNewKeyPressInfo(IsActive, KeyName, Key, Frequency: Frequency);
                    SetSimulationButtonEditableProperty(this.Simulation.IsRunning);
                }catch(Exception ex)
                {
                    Logger.Log(EventID.KeyPresserException, ex);
                }
            }
        }
        #endregion
        #region RemoveKeyPressInfo
        private void RemoveKeyPressInfo(KeyPressInfo toDelete)
        {
            try
            {
                if (this.Simulation != null && this.Simulation.RemoveKeyPressInfo(toDelete))
                    SetSimulationButtonEditableProperty(this.Simulation.IsRunning);
            }catch(Exception ex)
            {
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }
        #endregion
        #region GetPressedKey
        private Tuple<string, Key> GetPressedKey()
        {
            PressKeyDialog pressKeyDialog = new PressKeyDialog(this);
            if (pressKeyDialog.ShowDialog() == true)
            {
                return new Tuple<string, Key>(pressKeyDialog.PressedKeyText, pressKeyDialog.PressedKey);
            }
            return null;
        }
        #endregion
        #region SetSimulationControlsProperties
        private void SetSimulationControlsProperties(bool isRunning)
        {
            try
            {
                dgKeyPressConf.IsEnabled = !isRunning;
                if (isRunning)
                {
                    baSimulation.Label = "Stop";
                    baSimulation.SmallIcon = Utils.ToBitmapImage(ResourceImage48.Stop);
                    lvKeyPressLog.Items.Clear();
                }
                else
                {
                    baSimulation.Label = "Start";
                    baSimulation.SmallIcon = Utils.ToBitmapImage(ResourceImage48.Play);
                }
                SetSimulationButtonEditableProperty(isRunning);
            }catch(Exception ex)
            {
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }
        #endregion
        #region SetSimulationButtonEditableProperty
        private void SetSimulationButtonEditableProperty(bool isRunning)
        {
            try
            {
                if (isRunning)
                {
                    if (!baSimulation.IsEnabled)
                        baSimulation.IsEnabled = true;//na wszelki wypadek
                }
                else
                {
                    baSimulation.IsEnabled = this.Simulation != null && this.Simulation.HasActiveKeys();
                }
            }catch(Exception ex)
            {
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }
        #endregion
        #region ToogleSimulation
        private void ToogleSimulation()
        {
            try
            {
                if (this.Simulation != null)
                {
                    SetSimulationControlsProperties(!this.Simulation.IsRunning);

                    if (!this.Simulation.IsRunning)
                    {
                        #region SimulationStarted
                        this.Simulation.SetHazardBehaviour(clbiHazardBehaviour.IsSelected);
                        this.Simulation.Start();

                        this.tbKeyPressTest.Focus();

                        #endregion
                    }
                    else
                    {
                        this.Simulation.Stop();
                    }
                }
            }catch(Exception ex)
            {
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }
        #endregion

        #endregion

        #region Log

        private void Log(string log, bool threadSafeRequired = true)
        {
            try
            {
                if (threadSafeRequired)
                    lvKeyPressLog.Dispatcher.Invoke(new Action(() => { lvKeyPressLog.Items.Insert(0, log); }));
                else
                    lvKeyPressLog.Items.Insert(0, log);
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Logger.Log(EventID.KeyPresserException, ex);
            }
        }

        #endregion
    }
}
