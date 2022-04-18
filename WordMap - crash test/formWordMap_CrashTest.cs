using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WordMap___crash_test
{
    public partial class formWordMap_CrashTest : Form
    {
        public static formWordMap_CrashTest instance = null;
        #region scrollBarTool
        /// <summary>
        /// https://docs.microsoft.com/en-us/answers/questions/753327/send-mousescroll-from-picturebox-to-richtextbox.html
        /// this was used to let the mousewheel event on the WordMap picturebox simulate a MouseWheel event on RTX editor in use
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);
        System.Threading.Semaphore semScroll = new System.Threading.Semaphore(1, 1);
        #endregion

        Random rnd = new Random();

        public static RichTextBox rtxFocus = new RichTextBox();
        public static pictureboxWordAnalyzer picWordAnalyzer = new pictureboxWordAnalyzer();

        static CheckBox chxAutoTest = new CheckBox();

        static Timer tmrDelay = new Timer();
        static Timer tmrAutoTest = new Timer();

        public formWordMap_CrashTest()
        {
            instance = this;
            InitializeComponent();

            tmrDelay.Interval = 10;
            tmrDelay.Tick += TmrDelay_Tick;

            tmrAutoTest.Interval = 300;
            tmrAutoTest.Tick += TmrAutoTest_Tick;

            Controls.Add(chxAutoTest);
            chxAutoTest.Text = "Auto test";
            chxAutoTest.AutoSize = true;
            chxAutoTest.CheckedChanged += ChxAutoTest_CheckedChanged;

            Controls.Add(rtxFocus);
            rtxFocus.VScroll += RtxFocus_VScroll;
            rtxFocus.SelectionChanged += RtxFocus_SelectionChanged;
            rtxFocus_Load();

            Controls.Add(picWordAnalyzer);

            SizeChanged += FormWordMap_CrashTest_SizeChanged;
            Activated += FormWordMap_CrashTest_Activated;
        }

        private void TmrAutoTest_Tick(object sender, EventArgs e)
        {
            tmrAutoTest.Enabled = false;
            tmrAutOTest_SetInterval();
            int intSelectStart = rnd.Next(0, rtxFocus.Text.Length - 1);
            rtxFocus.Select(intSelectStart, 0);
            tmrAutoTest.Enabled = chxAutoTest.Checked;
        }
        void tmrAutOTest_SetInterval()
        {
            tmrAutoTest.Interval = (int)(rnd.Next(1, 1000));
            chxAutoTest.Text = "Auto Test(" + tmrAutoTest.Interval.ToString() + ")";
        }
        private void ChxAutoTest_CheckedChanged(object sender, EventArgs e)
        {
            tmrAutoTest.Enabled = chxAutoTest.Checked;
            tmrAutOTest_SetInterval();
        }

        private void RtxFocus_VScroll(object sender, EventArgs e)
        {
            semScroll.WaitOne();

            if (pictureboxWordAnalyzer.instance != null && pictureboxWordAnalyzer.instance.Visible)
                pictureboxWordAnalyzer.instance.Draw();

            semScroll.Release();
        }

        private void RtxFocus_SelectionChanged(object sender, EventArgs e)
        {
            rtxFocus_setWordSelected();
        }

        void rtxFocus_setWordSelected()
        {
            if (rtxFocus.Text.Length == 0) return;

            int intStart = rtxFocus.SelectionStart;
            if (intStart == rtxFocus.Text.Length)
                intStart = rtxFocus.Text.Length - 1;
            if (intStart < 0) return;


            char chrTest = rtxFocus.Text[intStart];

            while (!char.IsLetter(chrTest) && intStart > 0)
                chrTest = rtxFocus.Text[--intStart];

            int intEnd = intStart;

            while (char.IsLetter(chrTest) && intStart > 0)
                chrTest = rtxFocus.Text[--intStart];

            chrTest = rtxFocus.Text[intEnd];
            while (char.IsLetter(chrTest) && intEnd < rtxFocus.Text.Length - 1)
                chrTest = rtxFocus.Text[++intEnd];

            int intWord_Start = intStart + 1;
            int intWord_Length = intEnd - intStart - 1;
            if (intWord_Start >= 0 && intWord_Start + intWord_Length < rtxFocus.Text.Length && intWord_Length > 0)
            {
                Word = rtxFocus.Text.Substring(intWord_Start, intWord_Length);
                tmrDelay.Enabled = true;
            }

        }

        void rtxFocus_Load()
        {
            string[] strFiles = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), "*.rtf");
            if (strFiles.Length > 0)
                rtxFocus.LoadFile(strFiles[0]);
        }

        void placeObjects()
        {
            Size szForm = new Size(25, 45);

            chxAutoTest.Location = new Point(5, 5);
            rtxFocus.Location = new Point(chxAutoTest.Left, chxAutoTest.Bottom);
            picWordAnalyzer.Size = new Size(200, Height - chxAutoTest.Bottom - szForm.Height);
            picWordAnalyzer.Location = new Point(Width - szForm.Width - picWordAnalyzer.Width,
                                                 Height - szForm.Height - picWordAnalyzer.Height);
            rtxFocus.Size = new Size(picWordAnalyzer.Left - rtxFocus.Left, picWordAnalyzer.Height);

        }

        static string strWord = "";
        static string Word
        {
            get { return strWord; }
            set { strWord = value; }
        }


        private void TmrDelay_Tick(object sender, EventArgs e)
        {
            tmrDelay.Enabled = false;
            picWordAnalyzer.Word = Word;

            Text = pictureboxWordAnalyzer.intBackgroundCounter.ToString() + " >" + Word + "<";
        }


        public static string Result_MostRecent
        {
            get
            {
                return System.IO.Directory.GetCurrentDirectory() + "\\WordMap_Result_MostRecent.txt";
            }
        }

        public static string Results_Lists
        {
            get
            {
                return System.IO.Directory.GetCurrentDirectory() + "\\WordMap_Results_List.txt";
            }
        }


        bool bolInit = false;
        private void FormWordMap_CrashTest_Activated(object sender, EventArgs e)
        {
            if (bolInit) return;
            bolInit = true;
            if (System.IO.File.Exists(Result_MostRecent))
            {
                string strTestResult = System.IO.File.ReadAllText(Result_MostRecent);
                System.IO.File.AppendAllText(Results_Lists, "\r\n" + strTestResult);
            }

            placeObjects();
            chxAutoTest.Checked = true;
        }

        private void FormWordMap_CrashTest_SizeChanged(object sender, EventArgs e)
        {
            placeObjects();
        }

    }
}
