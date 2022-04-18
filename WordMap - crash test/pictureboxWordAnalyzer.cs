using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
namespace WordMap___crash_test
{
    public class pictureboxWordAnalyzer : PictureBox
    {
        System.Windows.Forms.Timer tmrDrawDelay = new System.Windows.Forms.Timer();

        Semaphore semDrawMap = new Semaphore(1, 1);
        
        public static pictureboxWordAnalyzer instance = null;
        public pictureboxWordAnalyzer()
        {
            instance = this;
            bckDrawWordMap.DoWork += BckDrawWordMap_DoWork;
            bckDrawWordMap.RunWorkerCompleted += BckDrawWordMap_RunWorkerCompleted;
            MouseWheel += PictureboxWordAnalyzer_MouseWheel;
            MouseClick += PictureboxWordAnalyzer_MouseClick;
            SizeChanged += PictureboxWordAnalyzer_SizeChanged;
            szPic = Size;
        }

        private void PictureboxWordAnalyzer_MouseClick(object sender, MouseEventArgs e)
        {
            Rectangle recPic = new Rectangle((Width - Image.Width) / 2,
                                             (Height - Image.Height) / 2,
                                             Image.Width,
                                             Image.Height);

            Point ptMouseRelPic = new Point(e.X - recPic.Left, e.Y - recPic.Top);

            //float fltY = (float)ptMouseRelPic.Y / (float)recPic.Height;

            if (rtxFocus != null)
            {
                int intErrorCounter = 0;
                bool bolMoveUp = false;
                do
                {
                    Point ptTopChar = rtxFocus.GetPositionFromCharIndex(0);
                    Point ptLastChar = rtxFocus.GetPositionFromCharIndex(rtxFocus.Text.Length - 1);

                    Point ptTL = rtxFocus.GetPositionFromCharIndex(0);

                    int intCharTL = rtxFocus.GetCharIndexFromPosition(new Point(0, 0));
                    int intCharBr = rtxFocus.GetCharIndexFromPosition(new Point(rtxFocus.Width, rtxFocus.Height));
                    Point ptVisible_TL = rtxFocus.GetPositionFromCharIndex(intCharTL);
                    Point ptVisible_BR = rtxFocus.GetPositionFromCharIndex(intCharBr);

                    int intRTX_TextHeight = (rtxFocus.GetPositionFromCharIndex(rtxFocus.Text.Length - 1)).Y - ptTL.Y + 17;

                    Rectangle recText = new Rectangle(0, 0, rtxFocus.Width, intRTX_TextHeight);
                    Rectangle recVisible_Source = new Rectangle(0, ptVisible_TL.Y - ptTL.Y, rtxFocus.Width, ptVisible_BR.Y - ptVisible_TL.Y);

                    double dblSizeFactor = (double)bmpMapBase.Height / (double)recText.Height;

                    Rectangle recDraw = new Rectangle(0,
                                                      (int)(recVisible_Source.Top * dblSizeFactor),
                                                      bmpMapBase.Width,
                                                      (int)(recVisible_Source.Height * dblSizeFactor));
                    int intRecDrawHeight_Tolerance = (int)((float)recDraw.Height * 0.3f);
                    //float fltHeightLine = 18f;
                    //float fltNumLines = (float)Math.Abs(recDraw.Top  + recDraw.Height /2 - ptMouseRelPic.Y) / fltHeightLine;

                    int intDelta = 120;// (int)(fltNumLines * 120f / (float)SystemInformation.MouseWheelScrollLines);

                    if (ptMouseRelPic.Y > recDraw.Top + intRecDrawHeight_Tolerance)
                    {
                        if (intErrorCounter >0 && !bolMoveUp)
                            return;
                        bolMoveUp = true;
                        PictureboxWordAnalyzer_MouseWheel((object)this, new MouseEventArgs(MouseButtons.None, 0, 0, 0, -intDelta));
                    }
                    else if (ptMouseRelPic.Y< recDraw.Bottom - intRecDrawHeight_Tolerance)
                    {
                        if (intErrorCounter > 0 && bolMoveUp)
                            return;
                        bolMoveUp = false;
                        PictureboxWordAnalyzer_MouseWheel((object)this, new MouseEventArgs(MouseButtons.None, 0, 0, 0, intDelta));
                    }
                    else
                    {
                        return;
                    }
                    intErrorCounter++;

                } while (intErrorCounter < 256);

            }

        }

        private void PictureboxWordAnalyzer_SizeChanged(object sender, EventArgs e)
        {
            szPic = Size;
        }


        static Size _szPic = new Size();

        public static Size szPic
        {
            get  { return _szPic; }
            set
            { _szPic = value; }
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/answers/questions/753327/send-mousescroll-from-picturebox-to-richtextbox.html
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureboxWordAnalyzer_MouseWheel(object sender, MouseEventArgs e)
        {
            
            if (rtxFocus != null)
            {
                int nNbLines = (int)((float)e.Delta * (float)SystemInformation.MouseWheelScrollLines / 120f);
                if (nNbLines > 0)
                {
                    for (int i = 0; i < nNbLines; i++)
                        formWordMap_CrashTest.SendMessage(rtxFocus.Handle, WM_VSCROLL, (int)MakeWord(SB_LINEUP, 0), IntPtr.Zero);
                }
                else
                {
                    for (int i = nNbLines; i < 0; i++)
                        formWordMap_CrashTest.SendMessage(rtxFocus.Handle, WM_VSCROLL, (int)MakeWord(SB_LINEDOWN, 0), IntPtr.Zero);
                }
            }
        }

        public static RichTextBox rtxFocus
        {
            get { return formWordMap_CrashTest.rtxFocus; }
        }

 
        public const int WM_VSCROLL = 0x0115; 
        public const int SB_LINEUP = 0;
        public const int SB_LINEDOWN = 1;

        public static uint MakeWord(byte low, byte high)
        {
            return ((uint)high << 8) | low;
        }


        private void BckDrawWordMap_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Draw();
            //groupboxWordAnalyzer.WordMap = true;

            //if (bolBackgroundWorker_RunAgain)
            //{
            //    bolBackgroundWorker_RunAgain = false;
            //    Word = Word;
            //}
        }
        class classRTX_Data
        {
            public Size Size = new Size();
            public string Rtf = "";
            public int SelectionStart = 0;
            public int SelectionLength = 0;
            public int ScrollCaretIndex = 0;
            public int RightMargin = 0;
            public classRTX_Data(ref RichTextBox ckRTX)
            {
                Size = ckRTX.Size;
                Rtf = ckRTX.Rtf;
                SelectionStart = ckRTX.SelectionStart;
                SelectionLength = ckRTX.SelectionLength;
                RightMargin = ckRTX.RightMargin;
                ScrollCaretIndex = ckRTX.GetCharFromPosition(new Point(0, 0));
            }

            public RichTextBox fromArgs(ref RichTextBox rtxRetVal)
            {
                if (rtxRetVal == null) return null;
                rtxRetVal.Size = Size;
                rtxRetVal.Rtf= Rtf;
                rtxRetVal.SelectionStart = SelectionStart;
                rtxRetVal.SelectionLength = SelectionLength;
                rtxRetVal.RightMargin = RightMargin;
                rtxRetVal.Select(ScrollCaretIndex, 0);
                rtxRetVal.ScrollToCaret();
                
                return rtxRetVal;
            }
        }

        public static int intBackgroundCounter = 0;
        private void BckDrawWordMap_DoWork(object sender, DoWorkEventArgs e)
        {
            RichTextBox rtxCopy = new RichTextBox();
            RichTextBox rtxCopy_Text = new RichTextBox();

            Point ptTopChar = new Point();
            Point ptLastChar = new Point();
            System.IO.File.WriteAllText(formWordMap_CrashTest.Result_MostRecent, (intBackgroundCounter++).ToString());
            System.Diagnostics.Debug.Print(intBackgroundCounter.ToString());

            try
            {
                classRTX_Data cRtxData = (classRTX_Data)e.Argument;

                cRtxData.fromArgs(ref rtxCopy_Text);
                cRtxData.fromArgs(ref rtxCopy);

                Bitmap bmpDraw = null;
                if (rtxCopy.Text.Length != 0 && strWord.Length > 0)
                {
                    rtxCopy_Text.Text = rtxCopy_Text.Text.ToUpper();
                    try
                    {
                        ptTopChar = rtxCopy.GetPositionFromCharIndex(0);                                    ///  this line or the next
                        ptLastChar = rtxCopy.GetPositionFromCharIndex(rtxCopy.Text.Length - 1);
                    }
                    catch (Exception)
                    {
                        bolBackgroundWorker_RunAgain = true;
                        return;
                    }

                    string strLastChar = rtxCopy.Text[rtxCopy_Text.Text.Length - 1].ToString();

                    Size szLastChar = TextRenderer.MeasureText(strLastChar, rtxCopy.Font);

                    Rectangle recSource = new Rectangle(0, 0, rtxCopy.Width, ptLastChar.Y - ptTopChar.Y + szLastChar.Height);

                    double dblFactorDraw = (double)szPic.Height / (double)recSource.Height;
                    Size szTextSource = TextRenderer.MeasureText(strWord, rtxCopy.Font);
                    Size szDrawRec = new Size((int)(szTextSource.Width * dblFactorDraw), (int)(szTextSource.Height * dblFactorDraw));

                    if (szDrawRec.Width < 3)
                        szDrawRec.Width = 3;
                    if (szDrawRec.Height < 2)
                        szDrawRec.Height = 2;
                    Size szBitmp = new Size((int)(recSource.Width * dblFactorDraw), (int)(recSource.Height * dblFactorDraw));

                    if (szBitmp.Width < 3) szBitmp.Width = 3;
                    if (szBitmp.Height < 3) szBitmp.Height = 3;

                    bmpDraw = new Bitmap(szBitmp.Width, szBitmp.Height);
                    int intCounter = 0;
                    using (Graphics g = Graphics.FromImage(bmpDraw))
                    {
                        g.FillRectangle(Brushes.White, new RectangleF(0, 0, bmpDraw.Width, bmpDraw.Height));
                        int intIndex = rtxCopy_Text.Text.IndexOf(strWord);
                        while (intIndex >= 0)
                        {
                            intCounter++;
                            char chrBefore = intIndex > 0
                                                      ? rtxCopy_Text.Text[intIndex - 1]
                                                      : ' ';
                            char chrAfter = intIndex + strWord.Length < rtxCopy_Text.Text.Length - 2
                                                                      ? rtxCopy_Text.Text[intIndex + strWord.Length]
                                                                      : ' ';

                            if (!Char.IsLetter(chrBefore) && !Char.IsLetter(chrAfter))
                            {
                                Point ptStart = rtxCopy.GetPositionFromCharIndex(intIndex);
                                Point ptDraw = new Point((int)(dblFactorDraw * ptStart.X), (int)(dblFactorDraw * (ptStart.Y - ptTopChar.Y)));
                                g.FillRectangle(Brushes.Red, new Rectangle(ptDraw, szDrawRec));
                            }

                            try
                            {
                                intIndex = rtxCopy_Text.Text.IndexOf(strWord, intIndex + 1);
                            }
                            catch (Exception)
                            {

                                intIndex = -1;
                            }
                        }
                    }
                    bmpDraw.MakeTransparent(Color.White);
                }
                _bmpMapBase = bmpDraw;
            }
            catch (Exception error)
            {

            }

            rtxCopy.Dispose();
            rtxCopy_Text.Dispose();
        }

        BackgroundWorker bckDrawWordMap = new BackgroundWorker();

        bool bolBackgroundWorker_RunAgain = false;
        string strWord = "";
        public string Word
        {
            get { return strWord; }
            set
            {
                value = clean_nonAlpha_Ends(value.ToUpper());
                if (value.Length > 0)
                {
                    //if (formWords.grbOptions.Toggle_WordMap_AutoShutOff)
                    //{
                    //    if (groupboxWordAnalyzer.WordMap)
                    //    {
                    //        groupboxWordAnalyzer.WordMap = false;
                    //        return;
                    //    }
                    //}

                    strWord = value;

                    DrawWordMap();
                }
            }
        }

        public static string clean_nonAlpha_Ends(string strSource)
        {
            return cleanBack_nonAlpha(cleanFront_nonAlpha(strSource));
        }

        public static string cleanFront_nonAlpha(string strSource)
        {
            while (strSource.Length > 0
                    &&
                 !char.IsLetter(strSource[0]))
            {
                strSource = strSource.Substring(1);
            }
            return strSource;
        }

        public static string cleanBack_nonAlpha(string strSource)
        {
            while (strSource.Length > 0
                    &&
                 !char.IsLetter(strSource[strSource.Length - 1]))
            {
                strSource = strSource.Substring(0, strSource.Length - 1);
            }
            return strSource;
        }
        public void DrawWordMap()
        {
            RichTextBox rtxRef = rtxFocus;
            classRTX_Data cRTX = new classRTX_Data(ref rtxRef);
            if (bckDrawWordMap.IsBusy)
            {
                bolBackgroundWorker_RunAgain = true;
                return;
            }
            bckDrawWordMap.RunWorkerAsync(cRTX);
        }


        public void Draw()
        {
            if (rtxFocus == null || bmpMapBase == null) return;
            // draw visible frame
            Point ptTL = rtxFocus.GetPositionFromCharIndex(0);

            int intCharTL = rtxFocus.GetCharIndexFromPosition(new Point(0, 0));
            int intCharBr = rtxFocus.GetCharIndexFromPosition(new Point(rtxFocus.Width, rtxFocus.Height));
            Point ptVisible_TL = rtxFocus.GetPositionFromCharIndex(intCharTL);
            Point ptVisible_BR = rtxFocus.GetPositionFromCharIndex(intCharBr);

            int intRTX_TextHeight = (rtxFocus.GetPositionFromCharIndex(rtxFocus.Text.Length - 1)).Y - ptTL.Y + 17;

            Rectangle recText = new Rectangle(0, 0, rtxFocus.Width, intRTX_TextHeight);
            Rectangle recVisible_Source = new Rectangle(0, ptVisible_TL.Y - ptTL.Y, rtxFocus.Width, ptVisible_BR.Y - ptVisible_TL.Y);

            double dblSizeFactor = (double)bmpMapBase.Height/(double)recText.Height ;

            Rectangle recDraw = new Rectangle(0,
                                              (int)(recVisible_Source.Top * dblSizeFactor),
                                              bmpMapBase.Width,
                                              (int)(recVisible_Source.Height * dblSizeFactor));

            if (recDraw.Width < 3)
                recDraw.Width = 3;
            if (recDraw.Height < 2)
                recDraw.Height = 2;
            
            Bitmap bmpOutput = new Bitmap(bmpMapBase.Width, bmpMapBase.Height);
            using (Graphics g = Graphics.FromImage(bmpOutput))
            {
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, bmpOutput.Width, bmpOutput.Height));
                g.FillRectangle(Brushes.LightGray, recDraw);
                g.DrawImage(bmpMapBase, new Point(0, 0));
            }
            Image = bmpOutput;
        }

        Bitmap bmpMapBase_Get(ref RichTextBox rtxFocus)
        {
            Bitmap bmpDraw = null;
            semDrawMap.WaitOne();
            if (rtxFocus.Text.Length != 0 && strWord.Length > 0)
            {
                using (RichTextBox rtxTemp = new RichTextBox()) // needed to set to UpperCase before searching word in text
                {
                    rtxTemp.Rtf = rtxFocus.Rtf;
                    rtxTemp.Text = rtxTemp.Text.ToUpper();
                    try
                    {
                        Point ptTopChar = rtxFocus.GetPositionFromCharIndex(0);
                        Point ptLastChar = rtxFocus.GetPositionFromCharIndex(rtxFocus.Text.Length - 1);

                        string strLastChar = rtxFocus.Text[rtxTemp.Text.Length - 1].ToString();

                        Size szLastChar = TextRenderer.MeasureText(strLastChar, rtxFocus.Font);

                        Rectangle recSource = new Rectangle(0, 0, rtxFocus.Width, ptLastChar.Y - ptTopChar.Y + szLastChar.Height);

                        double dblFactorDraw = (double)szPic.Height / (double)recSource.Height;
                        Size szTextSource = TextRenderer.MeasureText(strWord, rtxFocus.Font);
                        Size szDrawRec = new Size((int)(szTextSource.Width * dblFactorDraw), (int)(szTextSource.Height * dblFactorDraw));

                        if (szDrawRec.Width < 3)
                            szDrawRec.Width = 3;
                        if (szDrawRec.Height < 2)
                            szDrawRec.Height = 2;

                        bmpDraw = new Bitmap((int)(recSource.Width * dblFactorDraw), (int)(recSource.Height * dblFactorDraw));
                        int intCounter = 0;
                        using (Graphics g = Graphics.FromImage(bmpDraw))
                        {
                            g.FillRectangle(Brushes.White, new RectangleF(0, 0, bmpDraw.Width, bmpDraw.Height));
                            int intIndex = rtxTemp.Text.IndexOf(strWord);
                            while (intIndex >= 0)
                            {
                                intCounter++;
                                char chrBefore = intIndex > 0
                                                          ? rtxTemp.Text[intIndex - 1]
                                                          : ' ';
                                char chrAfter = intIndex + strWord.Length < rtxTemp.Text.Length - 2
                                                                          ? rtxTemp.Text[intIndex + strWord.Length]
                                                                          : ' ';

                                if (!Char.IsLetter(chrBefore) && !Char.IsLetter(chrAfter))
                                {
                                    Point ptStart = rtxFocus.GetPositionFromCharIndex(intIndex);
                                    Point ptDraw = new Point((int)(dblFactorDraw * ptStart.X), (int)(dblFactorDraw * (ptStart.Y - ptTopChar.Y)));
                                    g.FillRectangle(Brushes.Red, new Rectangle(ptDraw, szDrawRec));
                                }

                                try
                                {
                                    intIndex = rtxTemp.Text.IndexOf(strWord, intIndex + 1);
                                }
                                catch (Exception)
                                {

                                    intIndex = -1;
                                }
                            }
                        }
                        bmpDraw.MakeTransparent(Color.White);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            semDrawMap.Release();
            return bmpDraw;
        }

        Bitmap _bmpMapBase = null;
        Bitmap bmpMapBase
        {
            get
            {
                //if (_bmpMapBase == null)
                //{
                //    RichTextBox rtxCopy = new RichTextBox();
                //    rtxCopy.Size =rtxFocus.Size;
                //    rtxCopy.Rtf = rtxFocus.Rtf;
                //    rtxCopy.SelectionStart = rtxFocus.SelectionStart;
                //    rtxCopy.SelectionLength = rtxFocus.SelectionLength;
                //    rtxCopy.RightMargin = rtxFocus.RightMargin;
                //    rtxCopy.Select(rtxFocus.SelectionStart, rtxFocus.SelectionLength);
                //    rtxCopy.ScrollToCaret();
                //    _bmpMapBase = bmpMapBase_Get(ref rtxCopy);
                   
                //}
                return _bmpMapBase;
            }
        }
        //RichTextBox rtxFocus
        //{
        //    get
        //    {
        //        return groupboxWordAnalyzer.rtxFocus;
        //    }
        //}

    }

    
}
