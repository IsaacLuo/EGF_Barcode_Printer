using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Printing;
using System.Drawing.Printing;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace EGFBarcodePrinter
{
    public class PrintDialogHelper
    {
        public PrintDialogHelper()
        {
        }

        /// <summary>
        /// print the visual element of barcode
        /// </summary>
        /// <param name="element"></param>
        public void PrintVisual(FrameworkElement element)
        {
            var printDialog = new PrintDialog();
            SetPrintProperty(printDialog);
            var printQueue = SelectedPrintServer();
            if (printQueue != null)
            {
                printDialog.PrintQueue = printQueue;


                ////////////////////////////////////
                int w = 480;
                int h = 60;
                var renderTarget = new RenderTargetBitmap(w, h, 300, 300, PixelFormats.Default);
                printDialog.PrintTicket = new PrintTicket();
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(renderTarget.Width, renderTarget.Height);
                var capabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
                var scale = Math.Max(capabilities.PageImageableArea.ExtentWidth / w, capabilities.PageImageableArea.ExtentHeight / h);
                scale = 0.45;
                element.LayoutTransform = new ScaleTransform(scale, scale);
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                element.Measure(sz);
                element.Arrange(new Rect(new Point(0, 0), sz));
                ////////////////////////////////////
                printDialog.PrintVisual(element, "");
                var oldSize = new Size(480, 40);
                element.LayoutTransform = new ScaleTransform(1, 1);
                element.Measure(oldSize);
                element.Arrange(new Rect(new Point(0, 0), oldSize));
            }
        }

        /// <summary>
        /// setup printer
        /// </summary>
        /// <returns></returns>
        public PrintQueue SelectedPrintServer()
        {
            try
            {
                PrintServer printServer = null;
                printServer = new PrintServer();
                if (printServer == null) return null;//没有找到打印机服务器
                PrinterSettings settings = new PrinterSettings();
                var printQueue = printServer.GetPrintQueue(settings.PrinterName);
                return printQueue;
            }
            catch (Exception)
            {
                return null;//没有找到打印机
            }
        }

        /// <summary>
        /// printing format
        /// </summary>
        /// <param name="printDialog">document</param>
        /// <param name="pageSize">page size</param>
        /// <param name="pageOrientation">orientation portrait</param>
        public void SetPrintProperty(PrintDialog printDialog, System.Printing.PageMediaSizeName pageSize = PageMediaSizeName.ISOA4, PageOrientation pageOrientation = PageOrientation.Portrait)
        {
            var printTicket = printDialog.PrintTicket;
            // printTicket.PageMediaSize = new PageMediaSize(180,25);
            // printTicket.PageResolution = new PageResolution(300, 300);
            printTicket.PageBorderless = PageBorderless.Borderless;
            printTicket.PageOrientation = pageOrientation;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void pp_click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string plateType = button.Name.ToUpper();
            string dateString = DateTime.Now.ToString("yyMMdd");
            if (datepicker.SelectedDate.HasValue)
            {
                dateString = datepicker.SelectedDate.Value.ToString("yyMMdd");
            }
            string url = String.Format("https://ice.genomefoundry.org/ICE-REST/rest/barcode/date/{0}/{1}", plateType, dateString);

            var response = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>()));
            HttpContent x = response.Content;
            var jsonResponse = await x.ReadAsStringAsync();
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);
            string barcode = values["data"];
            this.printBarcode(barcode);
        }

        private void printBarcode(string text)
        {
            string barcode = Code128.Generate(text);
            label.Content = text;
            int barcodeEndY = 28;
            int barcodeEndX = 50;
            //int barcodeEndX = 10;
            printAreaCanvas.Children.Clear();
            int thick = 2;
            foreach (char c in barcode)
            {
                if (c == '1')
                {
                    LineGeometry myLineGeometry = new LineGeometry();
                    myLineGeometry.StartPoint = new Point(barcodeEndX, 0);
                    myLineGeometry.EndPoint = new Point(barcodeEndX, barcodeEndY);

                    Path myPath = new Path();
                    myPath.Stroke = Brushes.Black;
                    myPath.StrokeThickness = thick;
                    myPath.Data = myLineGeometry;

                    printAreaCanvas.Children.Add(myPath);
                }
                barcodeEndX += thick;
            }

        }

        private void printBarcode(object sender, RoutedEventArgs e)
        {
            var helper = new PrintDialogHelper();
            helper.PrintVisual(printArea);
        }

    }
}
