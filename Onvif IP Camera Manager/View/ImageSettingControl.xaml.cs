using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Onvif_IP_Camera_Manager.Model;
using Ozeki.Media.IPCamera;
using Ozeki.Media.IPCamera.Imaging;
using Ozeki.Media.IPCamera.Media;

namespace Onvif_IP_Camera_Manager.View
{
    /// <summary>
    /// Interaction logic for SettingName.xaml
    /// </summary>
    public partial class ImageSetting : UserControl, INotifyPropertyChanged
    {
        public ImageSetting()
        {
            SliderValues = new SliderModel();
            InitializeComponent();
        }

        public enum Type
        {
            Null, Brightness, Contrast, Saturation, BackLight, Sharpness, WhiteBalanceCb, WhiteBalanceCr, FrameRate
        }

        public Type SettingName
        {
            get { return (Type)GetValue(SettingNameProperty); }
            set { SetValue(SettingNameProperty, value); }
        }

        public static readonly DependencyProperty SettingNameProperty =
        DependencyProperty.Register("SettingName", typeof(Type), typeof(ImageSetting), new FrameworkPropertyMetadata(Type.Null, FrameworkPropertyMetadataOptions.AffectsRender));

        SliderModel GetSliderValues(Camera model)
        {
            if (model == null || model.CameraImage == null)
                return null;

            switch (SettingName)
            {
                case Type.Brightness:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (model.CameraImage.IsBrightnessSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.BrightnessInterval.Min,
                            Max = model.CameraImage.BrightnessInterval.Max,
                            Value = model.CameraImage.Brightness,
                        };
                    }
                    break;

                case Type.Contrast:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (model.CameraImage.IsContrastSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.ContrastInterval.Min,
                            Max = model.CameraImage.ContrastInterval.Max,
                            Value = model.CameraImage.Contrast
                        };
                    }
                    break;

                case Type.Saturation:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (model.CameraImage.IsColorSaturationSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.ColorSaturationInterval.Min,
                            Max = model.CameraImage.ColorSaturationInterval.Max,
                            Value = model.CameraImage.ColorSaturation
                        };
                    }
                    break;

                case Type.Sharpness:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (Model.CameraImage.IsSharpnessSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.SharpnessInterval.Min,
                            Max = model.CameraImage.SharpnessInterval.Max,
                            Value = model.CameraImage.Sharpness
                        };
                    }
                    break;

                case Type.BackLight:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (Model.CameraImage.IsBackLightCompensationSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.BackLightInterval.Min,
                            Max = model.CameraImage.BackLightInterval.Max,
                            Value = model.CameraImage.BackLightCompensation,
                        };
                    }
                    break;

                case Type.WhiteBalanceCb:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (Model.CameraImage.IsWhiteBalanceSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.WhiteBalanceYbGainInterval.Min,
                            Max = model.CameraImage.WhiteBalanceYbGainInterval.Max,
                            Value = model.CameraImage.WhiteBalance.CbGain
                        };
                    }
                    break;

                case Type.WhiteBalanceCr:
                    if (Model == null || Model.CameraImage == null) return new SliderModel();
                    if (Model.CameraImage.IsWhiteBalanceSupported)
                    {
                        return new SliderModel
                        {
                            Min = model.CameraImage.WhiteBalanceYrGainInterval.Min,
                            Max = model.CameraImage.WhiteBalanceYrGainInterval.Max,
                            Value = model.CameraImage.WhiteBalance.CrGain
                        };
                    }
                    break;

                case Type.FrameRate:
                    if (Model == null || Model.CurrentStream == null || Model.CurrentStream.VideoEncoding == null) return new SliderModel();
                    return new SliderModel
                    {
                        Min = 0,
                        Max = 100,
                        Value = Model.CurrentStream.VideoEncoding.FrameRate
                    };
            }
            return new SliderModel();
        }

        public SliderModel SliderValues { get; set; }

        public Camera Model
        {
            get { return (Camera)GetValue(ModelProperty); }
            set
            {
                InvokeGuiThread(() => SetValue(ModelProperty, value));

                SliderValues = GetSliderValues(value);
                OnPropertyChanged("SliderValues");
            }
        }

        public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register("Model", typeof(Camera), typeof(ImageSetting), new FrameworkPropertyMetadata(new Camera(), FrameworkPropertyMetadataOptions.AffectsRender));

        private void SliderPropertyValueChanged(object sender, DragCompletedEventArgs e)
        {

            if (Model == null) return;
            var slider = sender as Slider;
            var camera = Model.CameraObject as IIPCamera;
            if (slider == null || camera == null || camera.ImagingSettings == null)
                return;
            switch (SettingName)
            {
                case Type.Brightness:
                    Model.SetCameraImaging(new CameraImaging { Brightness = (int)slider.Value });
                    slider.Value = (int)camera.ImagingSettings.Brightness;
                    break;

                case Type.Contrast:
                    Model.SetCameraImaging(new CameraImaging { Contrast = (int)slider.Value });
                    slider.Value = (int)camera.ImagingSettings.Contrast;
                    break;

                case Type.Saturation:
                    Model.SetCameraImaging(new CameraImaging { ColorSaturation = (int)slider.Value });
                    slider.Value = (int)camera.ImagingSettings.ColorSaturation;
                    break;

                case Type.Sharpness:
                    Model.SetCameraImaging(new CameraImaging { Sharpness = (int)slider.Value });
                    slider.Value = (int)camera.ImagingSettings.Sharpness;
                    break;

                case Type.BackLight:
                    Model.SetCameraImaging(new CameraImaging { BackLightCompensation = (int)slider.Value });
                    slider.Value = (int)camera.ImagingSettings.BackLightCompensation;
                    break;

                case Type.WhiteBalanceCb:
                    Model.SetCameraImaging(new CameraImaging
                    {
                        WhiteBalance = new CameraWhiteBalance
                        {
                            CbGain = (int)slider.Value,
                            CrGain = Model.CameraImage.WhiteBalance.CrGain
                        }
                    });
                    slider.Value = (int)camera.ImagingSettings.WhiteBalance.CbGain;
                    break;

                case Type.WhiteBalanceCr:
                    Model.SetCameraImaging(new CameraImaging
                    {
                        WhiteBalance = new CameraWhiteBalance
                        {
                            CbGain = Model.CameraImage.WhiteBalance.CbGain,
                            CrGain = (int)slider.Value
                        }
                    });
                    slider.Value = (int)camera.ImagingSettings.WhiteBalance.CrGain;
                    break;

                case Type.FrameRate:
                    if (Model.CurrentStream.VideoEncoding == null) return;
                    Model.SetVideoEncoding(new IPCameraVideoEncoding { FrameRate = (int)slider.Value });
                    slider.Value = Model.CurrentStream.VideoEncoding.FrameRate;
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        void InvokeGuiThread(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }
    }

    public class SliderModel
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public float Value { get; set; }

        public SliderModel()
        {
            Min = 0;
            Max = 1;
            Value = 0;
        }
    }
}
