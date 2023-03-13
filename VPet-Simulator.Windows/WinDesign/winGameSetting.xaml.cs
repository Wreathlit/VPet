﻿using LinePutScript;
using Panuon.WPF.UI;
using Steamworks;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winGameSetting.xaml 的交互逻辑
    /// </summary>
    public partial class winGameSetting : WindowX
    {
        MainWindow mw;
        private bool AllowChange = false;
        public winGameSetting(MainWindow mw)
        {
            this.mw = mw;
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            InitializeComponent();
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            //var bit = new BitmapImage(new Uri("pack://application:,,,/Res/TopLogo2019.png"));
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            ////ImageWHY.Source = bit;
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            TopMostBox.IsChecked = mw.Set.TopMost;
            if (mw.Set.IsBiggerScreen)
            {
                FullScreenBox.IsChecked = true;
                ZoomSlider.Maximum = 8;
            }
            else
            {
                FullScreenBox.IsChecked = false;
                ZoomSlider.Maximum = 3;
            }
            ZoomSlider.Value = mw.Set.ZoomLevel * 2;
            //this.Width = 400 * Math.Sqrt(ZoomSlider.Value);
            //this.Height = 450 * Math.Sqrt(ZoomSlider.Value);

            sDesktopAlignment.IsChecked = mw.Set.EnableFunction;
            CalSlider.Value = mw.Set.LogicInterval;
            InteractionSlider.Value = mw.Set.InteractionCycle;
            MoveEventBox.IsChecked = mw.Set.AllowMove;
            SmartMoveEventBox.IsChecked = mw.Set.SmartMove;
            PressLengthSlider.Value = mw.Set.PressLength / 1000.0;

            StartUpBox.IsChecked = mw.Set.StartUPBoot;
            StartUpSteamBox.IsChecked = mw.Set.StartUPBootSteam;
            TextBoxPetName.Text = mw.Core.Save.Name;
            foreach (PetLoader pl in mw.Pets)
            {
                PetBox.Items.Add(pl.Name);
            }
            int petboxid = mw.Pets.FindIndex(x => x.Name == mw.Set.PetGraph);
            if (petboxid == -1)
                petboxid = 0;
            PetBox.SelectedIndex = petboxid;
            PetIntor.Text = mw.Pets[petboxid].Intor;

            foreach (ComboBoxItem v in CBAutoSave.Items)
            {
                if ((int)v.Tag == mw.Set.AutoSaveInterval)
                {
                    CBAutoSave.SelectedItem = v;
                    break;
                }
            }
            foreach (ComboBoxItem v in CBSmartMove.Items)
            {
                if ((int)v.Tag == mw.Set.SmartMoveInterval)
                {
                    CBSmartMove.SelectedItem = v;
                    break;
                }
            }

            foreach (Sub sub in mw.Set["diy"])
                StackDIY.Children.Add(new DIYViewer(sub));

#if X64
            GameVerison.Content = $"游戏版本v{mw.Verison} x64";
#else
            GameVerison.Content = $"游戏版本v{mw.Verison} x86";
#endif
            //关于ui
            if (mw.IsSteamUser)
            {
                runUserName.Text = Steamworks.SteamClient.Name;
                runActivate.Text = $"已通过Steam[{Steamworks.SteamClient.SteamId.Value:x}]激活服务注册";
            }
            else
            {
                runUserName.Text = Environment.UserName;
                runActivate.Text = "尚未激活 您可能需要启动Steam或去Steam上免费领个";
                RBCGPTUseLB.IsEnabled = false;
                if (!mw.Set["CGPT"][(gbol)"enable"])
                    BtnCGPTReSet.IsEnabled = false;
            }
            if (mw.Set["CGPT"][(gbol)"enable"])
            {
                RBCGPTUseAPI.IsChecked = true;
                BtnCGPTReSet.Content = "打开 ChatGPT API 设置";
            }
            else
                BtnCGPTReSet.Content = "初始化桌宠聊天程序";
            runabVer.Text = $"v{mw.Verison} ({mw.verison})";

            //mod列表
            ShowModList();
            ListMod.SelectedIndex = 0;
            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);

            AllowChange = true;
        }
        public void ShowModList()
        {
            ListMod.Items.Clear();
            foreach (CoreMOD mod in mw.CoreMODs)
            {
                ListBoxItem moditem = (ListBoxItem)ListMod.Items[ListMod.Items.Add(new ListBoxItem())];
                moditem.Content = mod.Name;
                if (mod.IsBanMOD(mw))
                {
                    moditem.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else
                {
                    if (mod.GameVer / 10 == mw.verison / 10)
                    {
                        moditem.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                    }
                    else
                    {
                        moditem.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                    }
                }
            }
        }
        CoreMOD mod;
        private void ShowMod(string modname)
        {
            mod = mw.CoreMODs.Find(x => x.Name == modname);
            LabelModName.Content = mod.Name;
            runMODAuthor.Text = mod.Author;
            runMODGameVer.Text = CoreMOD.INTtoVER(mod.GameVer);
            runMODGameVer.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
            ImageMOD.Source = new BitmapImage(new Uri(mod.Path.FullName + @"\icon.png"));
            if (mod.GameVer < mw.verison)
            {
                if (mod.GameVer / 10 == mw.verison / 10)
                {
                    runMODGameVer.Text += " (兼容)";
                }
                else
                {
                    runMODGameVer.Text += " (版本低)";
                    runMODGameVer.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                }
            }
            else if (mod.GameVer > mw.verison)
            {
                if (mod.GameVer / 10 == mw.verison / 10)
                {
                    runMODGameVer.Text += " (兼容)";
                    runMODGameVer.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                }
                else
                {
                    runMODGameVer.Text += " (版本高)";
                    runMODGameVer.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                }
            }
            if (mod.IsBanMOD(mw))
            {
                LabelModName.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                ButtonDisEnable.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.IsEnabled = true;
                ButtonDisEnable.IsEnabled = false;
            }
            else
            {
                LabelModName.Foreground = runMODGameVer.Foreground;
                ButtonDisEnable.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                ButtonEnable.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.IsEnabled = false;
                ButtonDisEnable.IsEnabled = true;
            }
            //发布steam等功能
            if (mw.IsSteamUser)
            {
                if (mod.ItemID == 1)
                {
                    ButtonSteam.IsEnabled = false;
                    ButtonPublish.Text = "系统自带";
                    ButtonSteam.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else if (mod.ItemID == 0)
                {
                    ButtonSteam.IsEnabled = false;
                    ButtonPublish.Text = "上传至Steam";
                    ButtonSteam.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else
                {
                    ButtonSteam.IsEnabled = true;
                    ButtonPublish.Text = "更新至Steam";
                    ButtonSteam.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                }
                if (mod.ItemID != 1 && (mod.AuthorID == Steamworks.SteamClient.SteamId.AccountId || mod.AuthorID == 0))
                {
                    ButtonPublish.IsEnabled = true;
                    ButtonPublish.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                }
                else
                {
                    ButtonPublish.IsEnabled = false;
                    ButtonPublish.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
            }
            else
            {
                ButtonSteam.IsEnabled = false;
                ButtonPublish.Text = "未登录";
                ButtonPublish.IsEnabled = false;
                ButtonPublish.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonSteam.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            }
            runMODVer.Text = CoreMOD.INTtoVER(mod.Ver);
            GameInfo.Text = mod.Intro;
            GameHave.Text = mod.Content.Trim('\n');

            ButtonAllow.Visibility = mod.SuccessLoad ? Visibility.Collapsed : Visibility.Visible;
        }
        private void FullScreenBox_Check(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (FullScreenBox.IsChecked == true)
            {
                mw.Set.IsBiggerScreen = true;
                ZoomSlider.Maximum = 8;
            }
            else
            {
                mw.Set.IsBiggerScreen = false;
                ZoomSlider.Maximum = 3;
            }
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CBAutoSave_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.AutoSaveInterval = (int)((ComboBoxItem)CBAutoSave.SelectedItem).Tag;
            if (mw.Set.AutoSaveInterval > 0)
            {
                mw.AutoSaveTimer.Interval = mw.Set.AutoSaveInterval * 60000;
                mw.AutoSaveTimer.Start();
            }
            else
            {
                mw.AutoSaveTimer.Stop();
            }
        }


        private void RBDiagnosisYES_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RBDiagnosisNO_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CBDiagnosis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ListMod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange || ListMod.SelectedItem == null)
                return;

            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);
        }

        private void ButtonOpenModFolder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(mod.Path.FullName);
        }

        private void ButtonEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mw.Set.BanModRemove(mod.Name);
            ShowMod((string)LabelModName.Content);
            ButtonRestart.Visibility = Visibility.Visible;
            //int seleid = ListMod.SelectedIndex();
            ShowModList();
        }

        private void ButtonDisEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mod.Name.ToLower() == "core")
            {
                MessageBoxX.Show("模组 Core 为<虚拟桌宠模拟器>核心文件,无法停用", "停用失败");
                return;
            }
            mw.Set.BanMod(mod.Name);
            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);
            ButtonRestart.Visibility = System.Windows.Visibility.Visible;
            ShowModList();
        }
        class ProgressClass : IProgress<float>
        {
            float lastvalue = 0;
            ProgressBar pb;
            public ProgressClass(ProgressBar p) => pb = p;
            public void Report(float value)
            {
                if (lastvalue >= value) return;
                lastvalue = value;
                pb.Dispatcher.Invoke(new Action(() => pb.Value = (int)(lastvalue * 100)));
            }
        }
        private async void ButtonPublish_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!mw.IsSteamUser)
            {
                MessageBoxX.Show("请先登录Steam后才能上传文件", "上传MOD需要Steam登录", MessageBoxIcon.Warning);
                return;
            }
            if (mod.Name.ToLower() == "core")
            {
                MessageBoxX.Show("模组 Core 为<虚拟桌宠模拟器>核心文件,无法发布\n如需发布自定义内容,请复制并更改名称", "MOD上传失败", MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(mod.Path.FullName + @"\icon.png") || new FileInfo(mod.Path.FullName + @"\icon.png").Length > 524288)
            {
                MessageBoxX.Show("封面图片(icon.png)大于500kb,请修改后重试", "MOD上传失败", MessageBoxIcon.Error);
                return;
            }
#if DEMO
            MessageBoxX.Show("经测试,除正式版均无创意工坊权限,此功能仅作为展示", "特殊版无法上传创意工坊");
#endif
            ButtonPublish.IsEnabled = false;
            ButtonPublish.Text = "正在上传";
            ProgressBarUpload.Visibility = Visibility.Visible;
            ProgressBarUpload.Value = 0;
            if (mod.ItemID == 0)
            {
                var result = Editor.NewCommunityFile
                        .WithTitle(mod.Name)
                        .WithDescription(mod.Intro)
                        .WithPublicVisibility()
                        .WithPreviewFile(mod.Path.FullName + @"\icon.png")
                        .WithContent(mod.Path.FullName);
                foreach (string tag in mod.Content.Trim('\n').Split('\n'))
                    result.WithTag(tag);
                var r = await result.SubmitAsync(new ProgressClass(ProgressBarUpload));
                mod.AuthorID = Steamworks.SteamClient.SteamId.AccountId;
                mod.WriteFile();
                if (r.Success)
                {
                    mod.ItemID = r.FileId.Value;
                    mod.WriteFile();
                    //ProgressBarUpload.Value = 0;
                    //await result.SubmitAsync(new ProgressClass(ProgressBarUpload));
                    if (MessageBoxX.Show($"{mod.Name} 成功上传至WorkShop服务器\n是否跳转至创意工坊页面进行编辑详细介绍和图标?", "MOD上传成功", MessageBoxButton.YesNo, MessageBoxIcon.Success) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=" + r.FileId);
                    }
                }
                else
                {
                    mod.AuthorID = 0; mod.WriteFile();
                    MessageBoxX.Show($"{mod.Name} 上传至WorkShop服务器失败\n请检查网络后重试\n请注意:上传和下载工坊物品可能需要良好的网络条件\n失败原因:{r.Result}", $"MOD上传失败 {r.Result}");
                }
            }
            else if (mod.AuthorID == Steamworks.SteamClient.SteamId.AccountId)
            {
                var result = new Editor(new Steamworks.Data.PublishedFileId() { Value = mod.ItemID })
                        .WithTitle(mod.Name)
                        .WithDescription(mod.Intro)
                        .WithPreviewFile(mod.Path.FullName + @"\icon.png")
                        .WithContent(mod.Path);
                foreach (string tag in mod.Content.Trim('\n').Split('\n'))
                    result.WithTag(tag);
                var r = await result.SubmitAsync(new ProgressClass(ProgressBarUpload));
                if (r.Success)
                {
                    mod.AuthorID = Steamworks.SteamClient.SteamId.AccountId;
                    mod.ItemID = r.FileId.Value;
                    mod.WriteFile();
                    if (MessageBoxX.Show($"{mod.Name} 成功上传至WorkShop服务器\n是否跳转至创意工坊页面进行编辑新内容?", "MOD更新成功", MessageBoxButton.YesNo, MessageBoxIcon.Success) == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=" + r.FileId);
                }
                else
                    MessageBoxX.Show($"{mod.Name} 上传至WorkShop服务器失败\n请检查网络后重试\n请注意:上传和下载工坊物品可能需要良好的网络条件\n失败原因:{r.Result}", $"MOD更新失败 {r.Result}");
            }
            ButtonPublish.IsEnabled = true;
            ButtonPublish.Text = "任务完成";
            ProgressBarUpload.Visibility = Visibility.Collapsed;
        }

        private void ButtonSteam_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AllowChange)
                return;
            var modname = (string)((ListBoxItem)ListMod.SelectedItem).Content;
            var mod = mw.CoreMODs.Find(x => x.Name == modname);
            System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=" + mod.ItemID);
        }

        private void ButtonAllow_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("是否退出游戏<虚拟桌宠模拟器>?\n请注意保存游戏", "重启游戏", MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.Yes)
            {
                mw.Restart();
            }
        }


        private void hyper_moreInfo(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.exlb.net/Diagnosis");
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mw.Topmost = mw.Set.TopMost;
            e.Cancel = true;
            Hide();
        }

        private void TopMostBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.TopMost = true;
        }

        private void TopMostBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.TopMost = false;
        }

        private void ZoomSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.SetZoomLevel(ZoomSlider.Value / 2);
            //this.Width = 400 * Math.Sqrt(ZoomSlider.Value);
            //this.Height = 450 * Math.Sqrt(ZoomSlider.Value);
        }

        private void PressLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.PressLength = (int)(PressLengthSlider.Value * 1000);
        }

        private void InteractionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.InteractionCycle = (int)(InteractionSlider.Value);
        }
        #region Link
        private void Git_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/LorisYounger/VPet/graphs/contributors");
        }

        private void Steam_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://store.steampowered.com/app/1920960/_/");
        }

        private void Github_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/LorisYounger/VPet");
        }

        private void LB_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.exlb.net/VPet");
        }

        private void VPET_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.exlb.net/");
        }
        private void VUP_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://store.steampowered.com/app/1352140/_/");
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://jq.qq.com/?_wv=1027&k=zmeWNHyI");
        }
        private void sendkey_click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.exlb.net/SendKeys");
        }
        #endregion
        private void sDesktopAlignment_Checked_1(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            MessageBoxX.Show("由于没做完,暂不支持数据计算\n敬请期待后续更新", "没做完!", MessageBoxButton.OK, MessageBoxIcon.Warning);
        }

        private void CalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.LogicInterval = CalSlider.Value;
            mw.Main.SetLogicInterval((int)(CalSlider.Value * 1000));
        }

        private void MoveEventBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.AllowMove = MoveEventBox.IsChecked == true;
            SetSmartMove();
        }

        private void SmartMoveEventBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SmartMove = SmartMoveEventBox.IsChecked == true;
            SetSmartMove();
        }

        private void CBSmartMove_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SmartMoveInterval = (int)((ComboBoxItem)CBSmartMove.SelectedItem).Tag;
            SetSmartMove();
        }
        public void SetSmartMove()
        {
            if (!AllowChange)
                return;
            mw.Main.SetMoveMode(mw.Set.AllowMove, mw.Set.SmartMove, mw.Set.SmartMoveInterval * 1000);
        }
        private void GenStartUP()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\VPET_Simulator.lnk";
            if (mw.Set.StartUPBoot)
            {
                if (File.Exists(path))
                    File.Delete(path);
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                string shortcutAddress;
                if (mw.Set.StartUPBootSteam)
#if DEMO
                    shortcutAddress = "steam://rungameid/2293870";
#else
                    shortcutAddress = "steam://rungameid/1920960";
#endif
                else
                    shortcutAddress = System.Reflection.Assembly.GetExecutingAssembly().Location;

                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(path);
                shortcut.Description = "VPet Simulator";
                shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                shortcut.TargetPath = shortcutAddress;
                shortcut.IconLocation = AppDomain.CurrentDomain.BaseDirectory + @"vpeticon.ico";
                shortcut.Save();
            }
            else
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        private void StartUpBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.StartUPBoot = StartUpBox.IsChecked == true;
            GenStartUP();
        }

        private void StartUpSteamBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.StartUPBootSteam = StartUpSteamBox.IsChecked == true;
            GenStartUP();
        }

        private void PetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.PetGraph = (string)PetBox.SelectedItem;
            PetIntor.Text = mw.Pets[PetBox.SelectedIndex].Intor;
            ButtonRestartGraph.Visibility = Visibility.Visible;
        }

        private void TextBoxPetName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Core.Save.Name = TextBoxPetName.Text;
        }

        private void DIY_ADD_Click(object sender, RoutedEventArgs e)
        {
            StackDIY.Children.Add(new DIYViewer());
        }

        private void DIY_Save_Click(object sender, RoutedEventArgs e)
        {
            mw.Set["diy"].Clear();
            foreach (DIYViewer dv in StackDIY.Children)
            {
                mw.Set["diy"].Add(dv.ToSub());
            }
            mw.LoadDIY();
        }


        private void ChatGPT_Reset_Click(object sender, RoutedEventArgs e)
        {
            if (mw.Set["CGPT"][(gbol)"enable"])
            {
                new winCGPTSetting(mw).ShowDialog();
            }
            else
            {
                string responseString = ((TalkBox)mw.TalkBox).ChatGPT_Reset();
                if (responseString == "SUCCESS")
                {
                    ((TalkBox)mw.TalkBox).btn_startup.Visibility = Visibility.Visible;
                    MessageBoxX.Show("桌宠重置成功");
                }
                else
                {
                    MessageBoxX.Show(responseString, "桌宠重置失败");
                }
            }
        }

        private void CGPType_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set["CGPT"].SetBool("enable", RBCGPTUseLB.IsChecked == false);
            if (mw.Set["CGPT"][(gbol)"enable"])
            {
                BtnCGPTReSet.Content = "打开 ChatGPT API 设置";
                BtnCGPTReSet.IsEnabled = true;
                if (mw.TalkBox != null)
                    mw.Main.ToolBar.MainGrid.Children.Remove(mw.TalkBox.This);
                mw.TalkBox = new TalkBoxAPI(mw);
                mw.Main.ToolBar.MainGrid.Children.Add(mw.TalkBox.This);
            }
            else
            {
                BtnCGPTReSet.Content = "初始化桌宠聊天程序";
                if (mw.TalkBox != null)
                    mw.Main.ToolBar.MainGrid.Children.Remove(mw.TalkBox.This);
                mw.TalkBox = new TalkBox(mw);
                mw.Main.ToolBar.MainGrid.Children.Add(mw.TalkBox.This);
            }
        }
    }
}
