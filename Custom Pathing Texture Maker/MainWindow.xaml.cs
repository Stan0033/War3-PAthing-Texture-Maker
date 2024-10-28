using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
 
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
 
using System.Windows.Shapes;

namespace Custom_Pathing_Texture_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int Columns = 40;
        int Rows = 40;
        System.Windows.Media.Brush[,] Grid = new System.Windows.Media.Brush[40, 40];
        public MainWindow() // constructor
        {
            InitializeComponent();
            Clearall(null, null);
            ReDraw();
           


        }
        string GetFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.png;*.tga;*.jpeg",

                Title = "Select an image"
            };

            // Show the dialog and check if a file is selected
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName; // Get the selected file path
                                                // Use the filePath as needed
            }
            return "";
        }
        public bool IsImageWithDimensions40x40OrLess(string filePath)
        {
            // Check if the file exists
            if (!File.Exists(filePath))
            {
                return false; // File does not exist
            }

            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load the image immediately
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Freeze to make it cross-thread accessible

                // Check if dimensions are 40x40 or less
                return bitmapImage.PixelWidth <= 40 && bitmapImage.PixelHeight <= 40;
            }
            catch (Exception)
            {
                return false; // Return false if the file is not a valid image or any other exception occurs
            }
        }


        private void open(object sender, RoutedEventArgs e)
        {
            string file = GetFile();
            if (file.Length > 0)
            {
                LoadGridFromBitmap(file);
            }
        }
        private void LoadGridFromBitmap(string filePath)
        {
            // Define the recognized colors
            var recognizedColors = new Dictionary<System.Windows.Media.Color, System.Windows.Media.Brush>
    {
        { Colors.White, System.Windows.Media.Brushes.White },
        { Colors.Transparent, System.Windows.Media.Brushes.Transparent },
        { Colors.Red, System.Windows.Media.Brushes.Red },
        { Colors.Yellow, System.Windows.Media.Brushes.Yellow },
        { Colors.Blue, System.Windows.Media.Brushes.Blue },
        { Colors.Green, System.Windows.Media.Brushes.Green },
        { Colors.Cyan, System.Windows.Media.Brushes.Cyan },
        { Colors.Magenta, System.Windows.Media.Brushes.Magenta }
    };

            // Initialize the Grid array with default colors (black)
            for (int i = 0; i < Grid.GetLength(0); i++)
            {
                for (int j = 0; j < Grid.GetLength(1); j++)
                {
                    Grid[i, j] = System.Windows.Media.Brushes.Black;
                }
            }

            // Check the file extension
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".tga")
            {
                MessageBox.Show("Unsupported file format. Please use JPG, PNG, or TGA.");
                return;
            }

            try
            {
                // Load the bitmap from the specified file path
                BitmapImage bitmap = new BitmapImage(new Uri(filePath, UriKind.Absolute));

                int width = Math.Min(40, bitmap.PixelWidth);
                int height = Math.Min(40, bitmap.PixelHeight);

                // Create a WriteableBitmap to access the pixel data
                WriteableBitmap writableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null); // 96 DPI for both axes

                // Copy the bitmap data into the WriteableBitmap
                writableBitmap.Lock();

                // Allocate the pixel array
                int[] pixels = new int[width * height];

                // Ensure we are reading the pixels in a compatible format
                bitmap.CopyPixels(new Int32Rect(0, 0, width, height), pixels, width * sizeof(int), 0);

                // Copy the pixel data to the WriteableBitmap
                Marshal.Copy(pixels, 0, writableBitmap.BackBuffer, pixels.Length);

                // Fill the Grid array with the pixel colors
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        System.Windows.Media.Color pixelColor = System.Windows.Media.Color.FromArgb(
                            (byte)((pixels[y * width + x] >> 24) & 0xFF), // A
                            (byte)((pixels[y * width + x] >> 16) & 0xFF), // R
                            (byte)((pixels[y * width + x] >> 8) & 0xFF),  // G
                            (byte)(pixels[y * width + x] & 0xFF)          // B
                        );

                        // Check if the pixel color is recognized, otherwise set to black
                        if (recognizedColors.TryGetValue(pixelColor, out System.Windows.Media.Brush brush))
                        {
                            Grid[y, x] = brush;
                        }
                        else
                        {
                            Grid[y, x] = System.Windows.Media.Brushes.Black; // Convert to black if unrecognized
                        }
                    }
                }

                writableBitmap.Unlock();

                // Call the ReDraw function to update the canvas
                ReDraw();
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, invalid format, etc.)
                MessageBox.Show($"Error loading bitmap: {ex.Message}");
            }
        }

        private void save(object sender, RoutedEventArgs e)
        {
            BitmapSource output = CreateBitmapFromGrid(Grid);
            if (Check_Outer.IsChecked == true) output = TrimBlackBorders(output);
            SaveBitmapSourceAsTga(output);

        }

        private void ChangedColumns(object sender, TextChangedEventArgs e)
        {
            bool parsed = int.TryParse(InputColumns.Text, out int columns);
            if (columns < 1) { Columns = 1; }
            else if (columns > 40) { Columns = 40; }
            else { Columns = columns; }
            ReDraw();

        }

        private void ChangedRows(object sender, TextChangedEventArgs e)
        {
            bool parsed = int.TryParse(InputRows.Text, out int rows);
            if (rows < 1) { Rows = 1; }
            else if (rows > 40) { Rows = 40; }
            else { Rows = rows; }
            ReDraw();
        }
        private void ReDraw()
        {
            // Configurable variables
            int maxRows = Rows;          // Use the provided Rows variable
            int maxColumns = Columns;    // Use the provided Columns variable
            double canvasSize = 400;     // Total canvas size
            double cellSize = 10;        // Cell size of 10 pixels

            // Clear existing children from the canvas
            CanvasGrid.Children.Clear();

            // Draw grid cells based on the colors in the Grid array
            for (int i = 0; i < Math.Min(maxRows, Grid.GetLength(0)); i++)
            {
                for (int j = 0; j < Math.Min(maxColumns, Grid.GetLength(1)); j++)
                {
                    // Create a rectangle for each cell
                    System.Windows.Media.Brush brush = Grid[i, j];
                    if (brush == System.Windows.Media.Brushes.Black) { brush = System.Windows.Media.Brushes.Transparent; }
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = cellSize,
                        Height = cellSize,

                        Fill = brush // Set the fill color based on the Grid
                    };

                    // Set the position of the rectangle
                    Canvas.SetLeft(rect, j * cellSize);
                    Canvas.SetTop(rect, i * cellSize);

                    // Add the rectangle to the canvas
                    CanvasGrid.Children.Add(rect);
                }
            }

            // Draw grid lines
            for (int i = 0; i <= maxColumns; i++)
            {
                double x = i * cellSize;
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    // Draw only to the height of existing rows or the full canvas size
                    X2 = x,
                    Y2 = (maxRows <= Grid.GetLength(0) ? maxRows * cellSize : canvasSize),
                    Stroke = System.Windows.Media.Brushes.Gray,
                    StrokeThickness = 1
                };
                CanvasGrid.Children.Add(line);
            }

            for (int j = 0; j <= maxRows; j++)
            {
                double y = j * cellSize;
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    // Draw only to the width of existing columns or the full canvas size
                    X2 = (maxColumns <= Grid.GetLength(1) ? maxColumns * cellSize : canvasSize),
                    Y2 = y,
                    Stroke = System.Windows.Media.Brushes.Gray,
                    StrokeThickness = 1
                };
                CanvasGrid.Children.Add(line);
            }
        }

        System.Windows.Media.Brush CurrentBrush = System.Windows.Media.Brushes.Black;
        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get the mouse click position
            System.Windows.Point mousePosition = e.GetPosition(CanvasGrid);

            // Calculate the cell size
            double cellSize = 400.0 / 40; // Assuming a fixed canvas size of 400 and 40 rows/columns

            // Calculate the row and column indices based on the mouse position
            int columnIndex = (int)(mousePosition.X / cellSize);
            int rowIndex = (int)(mousePosition.Y / cellSize);

            // Check if the indices are within the bounds of the Grid array
            if (columnIndex >= 0 && columnIndex < Grid.GetLength(1) &&
                rowIndex >= 0 && rowIndex < Grid.GetLength(0))
            {
                // Set the color of the clicked cell in the Grid to the CurrentBrush
                Grid[rowIndex, columnIndex] = CurrentBrush;

                // Optionally, redraw the canvas to reflect the updated color
                ReDraw();
            }
        }



        private void Clearall(object sender, RoutedEventArgs e)
        {
            int rows = 40; // Get number of rows
            int columns = 40; // Get number of columns

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    Grid[i, j] = System.Windows.Media.Brushes.Black; // Reset each element to Brushes.Black
                }
            }
            ReDraw();
        }

        private void SetCurrentBrush(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            CurrentBrush = b.Background;
            
        }

        

        private BitmapSource CreateBitmapFromGrid(System.Windows.Media.Brush[,] grid)
        {
            bool TrimOuterBlackPixels = Check_Outer.IsChecked == true;
            // Use Rows and Columns to determine the dimensions of the bitmap
            int width = Columns; // Number of columns
            int height = Rows;   // Number of rows

            // Create a WriteableBitmap
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            // Get the pixel data array
            int[] pixels = new int[width * height];

            // Fill the pixel data array based on the brushes in the Grid
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Ensure we don't go out of bounds of the Grid array
                    if (y < grid.GetLength(0) && x < grid.GetLength(1))
                    {
                        // Get the color from the brush
                        System.Windows.Media.Color color = GetColorFromBrush(grid[y, x]);
                        // Set the pixel color (in BGRA format)
                        pixels[y * width + x] = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
                    }
                    else
                    {
                        // Set pixels outside the grid to transparent
                        pixels[y * width + x] = 0;
                    }
                }
            }

            // Write the pixel data to the bitmap
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * sizeof(int), 0);

            return bitmap;
        }
        public int WpfColorToArgb(System.Windows.Media.Color color)
        {
            return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
        }
        private BitmapSource TrimBlackBorders(BitmapSource bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // Create a WriteableBitmap from the original BitmapSource
            WriteableBitmap writableBitmap = new WriteableBitmap(bitmap);
            int[] pixels = new int[width * height];

            // Copy pixel data to the array
            writableBitmap.CopyPixels(pixels, width * sizeof(int), 0);

            // Find the new width by trimming black columns from the right
            int newWidth = width;
            for (int x = width - 1; x >= 0; x--)
            {
                bool isColumnBlack = true;
                for (int y = 0; y < height; y++)
                {
                    if (pixels[y * width + x] != (Colors.Black.A << 24 | Colors.Black.B << 16 | Colors.Black.G << 8 | Colors.Black.R))
                    {
                        isColumnBlack = false;
                        break;
                    }
                }
                if (isColumnBlack)
                {
                    newWidth--;
                }
                else
                {
                    break; // Stop when we find a non-black column
                }
            }

            // Find the new height by trimming black rows from the bottom
            int newHeight = height;
            for (int y = height - 1; y >= 0; y--)
            {
                bool isRowBlack = true;
                for (int x = 0; x < newWidth; x++)
                {
                    if (pixels[y * width + x] != (Colors.Black.A << 24 | Colors.Black.B << 16 | Colors.Black.G << 8 | Colors.Black.R))
                    {
                        isRowBlack = false;
                        break;
                    }
                }
                if (isRowBlack)
                {
                    newHeight--;
                }
                else
                {
                    break; // Stop when we find a non-black row
                }
            }

            // Create a new WriteableBitmap for the trimmed image
            WriteableBitmap trimmedBitmap = new WriteableBitmap(newWidth, newHeight, bitmap.DpiX, bitmap.DpiY, PixelFormats.Bgra32, null);
            int[] trimmedPixels = new int[newWidth * newHeight];

            // Copy the relevant pixel data to the new bitmap
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    trimmedPixels[y * newWidth + x] = pixels[y * width + x];
                }
            }

            // Write the pixel data to the trimmed bitmap
            trimmedBitmap.WritePixels(new Int32Rect(0, 0, newWidth, newHeight), trimmedPixels, newWidth * sizeof(int), 0);

            return trimmedBitmap; // Return the new trimmed bitmap
        }

        private System.Windows.Media.Color GetColorFromBrush(System.Windows.Media.Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                if (brush == System.Windows.Media.Brushes.Black) { return Colors.Black; }
                return solidColorBrush.Color;
            }
            // If it's not a SolidColorBrush, return transparent by default
            return Colors.Transparent;
        }


        public void SaveBitmapSourceAsTga(BitmapSource bitmapSource)
        {
            // Prompt the user with a save file dialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "TGA Image|*.tga",
                Title = "Save an Image"
            };

            // Show the dialog and check if the user clicked "Save"
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // Define the width and height of the image
                int width = bitmapSource.PixelWidth;
                int height = bitmapSource.PixelHeight;

                // Convert the BitmapSource to a 24-bit RGB format
                FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgr24, null, 0);

                // Calculate the number of bytes needed for the pixel data
                int stride = width * 3; // 3 bytes per pixel for 24-bit RGB
                byte[] pixelData = new byte[stride * height];

                // Copy the pixels from the converted bitmap into the pixelData array
                convertedBitmap.CopyPixels(pixelData, stride, 0);

                // Open a file stream to write the TGA file
                using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    // Write the TGA header
                    writer.Write((byte)0);                  // ID length
                    writer.Write((byte)0);                  // Color map type
                    writer.Write((byte)2);                  // Image type (uncompressed true-color image)
                    writer.Write((ushort)0);                // First entry index
                    writer.Write((ushort)0);                // Color map length
                    writer.Write((byte)0);                  // Color map entry size
                    writer.Write((ushort)0);                // X-origin
                    writer.Write((ushort)0);                // Y-origin
                    writer.Write((ushort)width);            // Image width
                    writer.Write((ushort)height);           // Image height
                    writer.Write((byte)24);                 // Pixel depth (24 bits per pixel)
                    writer.Write((byte)0);                  // Image descriptor (no alpha channel, origin in lower-left corner)

                    // Write the pixel data in BGR order
                    writer.Write(pixelData);
                }
            }
        }

        private void Checked_showImage(object sender, RoutedEventArgs e)
        {
            ComparisonImage.Visibility = Check_BG.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        ImageSource CurrentImageForComparison = null;


        private void loadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.bmp;*.jpg;*.tga",
                Title = "Open an Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                // Step 1: Get the original dimensions of the image file using System.Drawing
                int originalWidth, originalHeight;

                using (var img = System.Drawing.Image.FromFile(filePath))
                {
                    originalWidth = img.Width;
                    originalHeight = img.Height;
                }
                if (originalHeight > 400 || originalWidth > 400)
                {
                    MessageBox.Show("Loaded image cannot be greater than 400x400. 1 grid pixel = 40 image pixels."); return;
                }
                // Step 2: Load the image into a Bitmap
                using (var originalBitmap = new Bitmap(filePath))
                {
                    // Step 3: Check if the dimensions match
                    if (originalBitmap.Width != originalWidth || originalBitmap.Height != originalHeight)
                    {
                        // Step 4: Resize the image to the original dimensions
                        using (var resizedBitmap = new Bitmap(originalWidth, originalHeight))
                        {
                            using (Graphics g = Graphics.FromImage(resizedBitmap))
                            {
                                // Draw the original image onto the resized Bitmap
                                g.DrawImage(originalBitmap, 0, 0, originalWidth, originalHeight);
                            }
                            // Convert the resized Bitmap to ImageSource
                            CurrentImageForComparison = ConvertBitmapToImageSource(resizedBitmap);
                            SetImage( );
                        }
                    }
                    else
                    {
                        // No resizing needed, convert directly to ImageSource
                        CurrentImageForComparison = ConvertBitmapToImageSource(originalBitmap);
                        SetImage( );
                    }
                }
            }
        }

        private ImageSource ConvertBitmapToImageSource(Bitmap bitmap)
        {
            // Convert Bitmap to BitmapSource
            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // Cleanup
            DeleteObject(bitmap.GetHbitmap()); // Release the HBitmap resource
            return bitmapSource;
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        private void GetSystemDPI()
        {
            // Get the main window's presentation source
            var mainWindow = Application.Current.MainWindow;
            var source = PresentationSource.FromVisual(mainWindow);

            if (source?.CompositionTarget != null)
            {
                // DPI X and Y scaling factors
                double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                double dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

                MessageBox.Show($"System DPI: {dpiX} x {dpiY}");
            }
            else
            {
                MessageBox.Show("Unable to determine DPI.");
            }
        }

        private void SetImage()
        {
            if (CurrentImageForComparison == null)
            {
                ComparisonImage2.Visibility = Visibility.Collapsed;
                ComparisonImage2.Source = null;
            }
            else
            {
               
                ComparisonImage2.Height = CurrentImageForComparison.Height;
                ComparisonImage2.Width = CurrentImageForComparison.Width;
                ComparisonImage2.Source = CurrentImageForComparison;
                ComparisonImage2.Visibility = Visibility.Visible;
            }
        }

        private void UnloadImage(object sender, RoutedEventArgs e)
        {
            CurrentImageForComparison = null;
            ComparisonImage2.Source = null;
            ComparisonImage2.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           // GetSystemDPI();
        }
    }
}
 