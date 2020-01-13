using System;
using Android;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace XamarinAndroidBLENotifyTest
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Plugin.BLE.Abstractions.Contracts.IAdapter adapter;
        public const string ServiceUUID = "4FAFC201-1FB5-459E-8FCC-C5C9C331914B";


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Android.Content.PM.Permission.Granted)
            {
                Android.Util.Log.Debug("BLE", "PERMIT OF LOCATION ALEADY SUTISFIED");
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation }, 1);
            }

            var ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            var state = ble.State;
            Android.Util.Log.Debug("BLE", state.ToString());
            bleScan();

        }

        void Characteristic_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            string uuid = e.Characteristic.Uuid.ToUpper();
            Android.Util.Log.Debug("BLE", "Notify Received. " + e.Characteristic.Value[0].ToString() + " (" + uuid + ")");
            TextView text = FindViewById<TextView>(Resource.Id.textView1);
            this.RunOnUiThread(() =>
            {
                if (e.Characteristic.Value[0] == 0)
                {
                    text.Text = "OFF";
                }
                else
                {
                    text.Text = "ON";
                }
            });
        }


        private async void bleScan()
        {
            IDevice esp32 = null;
            while (esp32 == null)
            {
                Android.Util.Log.Debug("BLE", "Scanning");
                adapter.ScanTimeout = 10000;
                adapter.ScanMode = ScanMode.LowLatency;
                adapter.DeviceDiscovered += async (s, a) =>
                {
                    Android.Util.Log.Debug("BLE", a.Device.Id.ToString() + " (" + a.Device.Name + ")");
                    if(a.Device.Id.ToString() == "00000000-0000-0000-0000-d8a01d463ef2")
                    {
                        Android.Util.Log.Debug("BLE", "ESP32 Found");
                        esp32 = a.Device;
                    }
                };
                await adapter.StartScanningForDevicesAsync();
            }
            if(esp32 != null)
            {
                await adapter.StopScanningForDevicesAsync();
                await adapter.ConnectToDeviceAsync(esp32);
                if (esp32.State == DeviceState.Disconnected)
                {
                    Android.Util.Log.Debug("BLE", "ESP32 Connect failed");
                }
                if (esp32.State == DeviceState.Connected || esp32.State == DeviceState.Limited)
                {
                    Android.Util.Log.Debug("BLE", "ESP32 Connected");
                    IService svc = await esp32.GetServiceAsync(new Guid(ServiceUUID.ToLower()));
                    var chr = await svc.GetCharacteristicsAsync();
                    foreach (var characteristic in chr)
                    {
                        var uuid = characteristic.Uuid.ToUpper();

                        if (characteristic.CanUpdate)
                        {
                            await characteristic.StartUpdatesAsync();
                            characteristic.ValueUpdated += Characteristic_ValueUpdated;
                        }
                    }

                }
            }

        }



        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

