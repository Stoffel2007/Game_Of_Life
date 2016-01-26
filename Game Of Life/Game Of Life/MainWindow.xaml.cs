using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Game_Of_Life
{
    public partial class MainWindow : Window
    {
        bool[,] field;
        FieldList previousFields, nextFields;
        Rectangle[,] rectangleArray;
        bool borderIsActive, continueOnMouseRelease;
        string ruleStringSurvive, ruleStringCreate, errorMessage;
        int maxFieldSize;
        double gameBoardLength, gameBoardWidth;
        DispatcherTimer[] timers;
        BitmapImage path_icon_start, path_icon_stop, path_icon_border_on, path_icon_border_off;

        public MainWindow()
        {
            InitializeComponent();

            field = null;
            previousFields = new FieldList();
            nextFields = new FieldList();
            rectangleArray = null;
            borderIsActive = false;
            continueOnMouseRelease = false;
            ruleStringSurvive = "23";
            ruleStringCreate = "3";
            errorMessage = "";
            maxFieldSize = 99;
            gameBoardLength = 655;
            gameBoardWidth = 655;

            InitializeButtonImages();
            InitializeTimers(3);
            adjustSizeOfInputBoxes();
            InitializePatternClicks();
            InitializePatternTags();

            MaxHeight = Height;
            MinHeight = Height;
            MaxWidth = Width;
            MinWidth = Width;

            textblock_current_rule.ToolTip = "The first value is a list of all the numbers of neighbours\n"
                                           + "that cause a living cell to keep alive.\n\n"
                                           + "The second value is a list of all the numbers of neighbours\n"
                                           + "that cause a dead cell to become alive.";

            foreach (MenuItem item in menu_rules.Items)
            {
                item.Header += " (" + item.Tag.ToString() + ")";
                item.Click += newRuleSelected;
            }

            textbox_field_width.Focus();
        }

        // Buttons
        #region
        private void button_draw_Click(object sender, EventArgs e)
        {
            toggleTimer();
            
            zoom.ScaleX = 1;
            zoom.ScaleY = 1;

            int fieldWidth = getValueFromTextbox(textbox_field_width, "Width");
            int fieldLength = getValueFromTextbox(textbox_field_length, "Length");

            if (fieldLength > 0 && fieldWidth > 0)
            {
                bool[,] fieldBefore = field;
                field = getRandomField(fieldLength, fieldWidth);
                updateFieldLists(fieldBefore);
                changeContentOfPatternTextblock("None");

                button_play_pause.IsEnabled = true;
                button_step.IsEnabled = true;
                button_delete.IsEnabled = true;

                drawNewField();
                printField();
            }
            else
            {
                MessageBox.Show(errorMessage, "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                errorMessage = "";
            }
        }

        private void button_delete_field_Click(object sender, EventArgs e)
        {
            toggleTimer();

            if (field != null)
            {
                textblock_current_pattern.Text = "Pattern: None";
                bool[,] fieldBefore = field;

                field = getEmptyField(field.GetLength(0), field.GetLength(1));
                updateFieldLists(fieldBefore);

                printField();
            }
        }

        private void button_fast_rewind_Click(object sender, EventArgs e)
        {
            toggleTimer(0);
        }

        private void button_step_rewind_Click(object sender, EventArgs e)
        {
            toggleTimer();
            rewind(sender, e);
        }

        private void button_play_pause_Click(object sender, EventArgs e)
        {
            toggleTimer(1);
        }

        private void button_step_Click(object sender, EventArgs e)
        {
            toggleTimer();
            nextRound(sender, e);
        }

        private void button_step_forward_Click(object sender, EventArgs e)
        {
            toggleTimer();
            forward(sender, e);
        }

        private void button_fast_forward_Click(object sender, EventArgs e)
        {
            toggleTimer(2);
        }

        private void button_toggle_gridlines_Click(object sender, EventArgs e)
        {
            toggle_gridlines(sender, e);
        }

        private void button_toggle_border_Click(object sender, EventArgs e)
        {
            toggle_border(sender, e);
        }
        #endregion

        // Text Fields
        #region
        private void textbox_GotKeyboardFocus(object sender, EventArgs e)
        {
            TextBox box = (TextBox)sender;
            box.SelectAll();
        }

        private void changeContentOfRuleTextblock(object sender, string text = "")
        {
            if (text == "")
                text = (sender as MenuItem).Header.ToString();
            else
                text += " (" + ruleStringSurvive + "/" + ruleStringCreate + ")";
            textblock_current_rule.Text = text;
        }

        private void changeContentOfPatternTextblock(string patternName)
        {
            textblock_current_pattern.Text = patternName + " (" + field.GetLength(1) + "x" + field.GetLength(0) + ")";
        }

        private void NumericEditPreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9);

            if ((isNumeric || isNumPadNumeric) && Keyboard.Modifiers != ModifierKeys.None)
            {
                e.Handled = true;
                return;
            }

            bool isControl = ((Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Insert || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right
                || e.Key == Key.Up || e.Key == Key.Tab || e.Key == Key.PageDown || e.Key == Key.PageUp || e.Key == Key.Enter || e.Key == Key.Return
                || e.Key == Key.Escape || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Q || e.Key == Key.E || e.Key == Key.A || e.Key == Key.S
                || e.Key == Key.D || e.Key == Key.I || e.Key == Key.J || e.Key == Key.K || e.Key == Key.L || e.Key == Key.Y || e.Key == Key.X);

            e.Handled = !isControl && !isNumeric && !isNumPadNumeric;
        }

        private void adjustSizeOfInputBoxes()
        {
            int maxInputLength = maxFieldSize.ToString().Length;
            textbox_field_length.MaxLength = maxInputLength;
            textbox_field_width.MaxLength = maxInputLength;
            int inputFieldWidth = maxInputLength * 10;
            textbox_field_length.Width = inputFieldWidth;
            textbox_field_width.Width = inputFieldWidth;
            stackpanel_input_field_size.Width = 40 + inputFieldWidth;
        }

        private int getValueFromTextbox(TextBox element, string name)
        {
            int number = -1;
            string tempMessage = "";

            if (element.Text == "")
                tempMessage = name + " ist leer\n";
            else
            {
                number = Convert.ToInt32(element.Text);
                if (number == 0)
                    tempMessage = name + " darf nicht 0 sein\n";
                else if (number > maxFieldSize)
                {
                    tempMessage = name + " darf höchstens " + maxFieldSize + " sein\n";
                    number = 0;
                }
            }

            if (tempMessage != "")
            {
                if (errorMessage == "")
                    element.Focus();
                errorMessage += tempMessage;
                element.BorderBrush = Brushes.Red;
                element.BorderThickness = new Thickness(2);
            }
            else
            {
                element.BorderBrush = Brushes.Black;
                element.BorderThickness = new Thickness(1);
            }

            return number;
        }
        #endregion

        // Other UI elements
        #region
        private void toggle_gridlines(object sender, EventArgs e)
        {
            gameboard.ShowGridLines = !gameboard.ShowGridLines;
        }

        private void toggle_border(object sender, EventArgs e)
        {
            borderIsActive = !borderIsActive;
            setImageBorderButton();
        }

        private void gameboardZoom(object sender, MouseWheelEventArgs e)
        {
            int diff = e.Delta / 120;
            int maxScale = Math.Max(field.GetLength(0), field.GetLength(1)) / 10;
            double scale = zoom.ScaleX + diff;

            if (scale < 1)
                scale = 1;
            else if (scale > maxScale)
                scale = maxScale;
            else
            {
                Point mousePosition = Mouse.GetPosition(gameboard);
                zoom.CenterX = mousePosition.X;
                zoom.CenterY = mousePosition.Y;
            }

            zoom.ScaleX = scale;
            zoom.ScaleY = scale;
        }

        private void gameboardMove(object sender, ExecutedRoutedEventArgs e)
        {
            if (field != null)
            {
                double x = zoom.CenterX;
                double y = zoom.CenterY;
                int diff = 10;

                if (Keyboard.IsKeyDown(Key.J))
                    x -= diff;
                if (Keyboard.IsKeyDown(Key.L))
                    x += diff;
                if (Keyboard.IsKeyDown(Key.I))
                    y -= diff;
                if (Keyboard.IsKeyDown(Key.K))
                    y += diff;

                if (x < 0)
                    x = 0;
                if (x > gameBoardWidth)
                    x = gameBoardWidth;
                if (y < 0)
                    y = 0;
                if (y > gameBoardLength)
                    y = gameBoardLength;

                zoom.CenterX = x;
                zoom.CenterY = y;
            }
        }

        private void updateMultiplyComboboxes()
        {
            int length = field.GetLength(0);
            int width = field.GetLength(1);
            multiply_field_downwards.Items.Clear();
            multiply_field_downwards.IsEnabled = false;
            multiply_field_sidewards.Items.Clear();
            multiply_field_sidewards.IsEnabled = false;
            int maxFactorLength = maxFieldSize / length;
            int maxFactorWidth = maxFieldSize / width;

            ComboBoxItem headerLength = new ComboBoxItem();
            headerLength.Content = "multiply";
            headerLength.Visibility = Visibility.Collapsed;
            headerLength.IsSelected = true;

            ComboBoxItem headerWidth = new ComboBoxItem();
            headerWidth.Content = "multiply";
            headerWidth.Visibility = Visibility.Collapsed;
            headerWidth.IsSelected = true;

            multiply_field_downwards.Items.Add(headerLength);
            multiply_field_sidewards.Items.Add(headerWidth);

            multiply_field_downwards.IsEnabled = maxFactorLength > 1;
            multiply_field_sidewards.IsEnabled = maxFactorWidth > 1;

            for (int i = 2; i <= maxFactorLength; i++)
            {
                ComboBoxItem newItem = new ComboBoxItem();
                newItem.Content = "x" + i;
                newItem.Tag = i;
                // currently not working yet
                // newItem.Selected += multiplyFieldDownwards;
                multiply_field_downwards.Items.Add(newItem);
            }

            for (int i = 2; i <= maxFactorWidth; i++)
            {
                ComboBoxItem newItem = new ComboBoxItem();
                newItem.Content = "x" + i;
                newItem.Tag = i;
                // currently not working yet
                // newItem.Selected += multiplyFieldSidewards;
                multiply_field_sidewards.Items.Add(newItem);
            }
        }

        private void setImageBorderButton()
        {
            if (borderIsActive)
                image_toggle_border.Source = path_icon_border_on;
            else
                image_toggle_border.Source = path_icon_border_off;
        }
        #endregion

        // Drawing/Erasing
        #region
        private void drawCell(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                createCell(sender, e);
            else if (e.RightButton == MouseButtonState.Pressed)
                killCell(sender, e);
        }

        private void createCell(object sender, EventArgs e)
        {
            colorizeCell(sender, Brushes.Black);
        }

        private void killCell(object sender, EventArgs e)
        {
            colorizeCell(sender, Brushes.White);
        }

        private void colorizeCell(object sender, SolidColorBrush color)
        {
            if (timers[1].IsEnabled)
            {
                timers[1].Stop();
                continueOnMouseRelease = true;
            }
            bool[,] fieldBefore = new bool[field.GetLength(0), field.GetLength(1)];

            for (int i = 0; i < field.GetLength(0); i++)
            {
                for (int j = 0; j < field.GetLength(1); j++)
                    fieldBefore[i, j] = field[i, j];
            }

            Rectangle temp = (Rectangle)sender;
            int[] coordinates = (int[])temp.Tag;
            int row = coordinates[0];
            int column = coordinates[1];


            int numOfCells = Convert.ToInt32(textblock_number_of_cells.Text);

            if (temp.Fill != color)
            {
                if (color == Brushes.Black)
                    numOfCells++;
                else
                    numOfCells--;
            }
            textblock_number_of_cells.Text = (numOfCells).ToString();

            temp.Fill = color;
            field[row, column] = color == Brushes.Black;

            bool timerIsRunning = timers[0].IsEnabled;

            updateFieldLists(fieldBefore);

            if (timerIsRunning)
            {
                image_play_pause.Source = path_icon_stop;
                timers[0].Start();
            }
        }

        private void continueGame(object sender, EventArgs e)
        {
            if (continueOnMouseRelease)
            {
                toggleTimer(1);
                continueOnMouseRelease = false;
            }
        }
        #endregion

        // Program logic
        #region
        private bool isAliveNextRound(bool isAlive, int numberOfNeighbours)
        {
            if (isAlive)
            {
                foreach (char c in ruleStringSurvive)
                {
                    if (numberOfNeighbours.ToString() == c.ToString())
                        return true;
                }
            }
            else
            {
                foreach (char c in ruleStringCreate)
                {
                    if (numberOfNeighbours.ToString() == c.ToString())
                        return true;
                }
            }
            return false;
        }

        private bool[,] getFieldOfNextRound()
        {
            int length = field.GetLength(0);
            int width = field.GetLength(1);
            bool[,] output = new bool[length, width];
            int numberOfNeighbours;
            bool isAlive;

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    numberOfNeighbours = getNumberOfNeighbours(i, j);
                    isAlive = field[i, j];
                    output[i, j] = isAliveNextRound(isAlive, numberOfNeighbours);
                }
            }

            return output;
        }

        private int getNumberOfNeighbours(int line, int column)
        {
            int numberOfNeighbours = 0;
            int fieldLength = field.GetLength(0);
            int fieldWidth = field.GetLength(1);

            for (int i = line - 1; i <= line + 1; i++)
            {
                int temp_i = i;
                if (!borderIsActive)
                    i = setIndexIntoFieldRange(i, fieldLength);
                else if (i < 0 || i >= fieldLength)
                    continue;

                for (int j = column - 1; j <= column + 1; j++)
                {
                    int temp_j = j;
                    if (!borderIsActive)
                        j = setIndexIntoFieldRange(j, fieldWidth);
                    else if (j < 0 || j >= fieldWidth)
                        continue;
                    if (field[i, j])
                        numberOfNeighbours++;
                    j = temp_j;
                }

                i = temp_i;
            }

            if (field[line, column])
                numberOfNeighbours--;

            return numberOfNeighbours;
        }

        private int setIndexIntoFieldRange(int index, int max)
        {
            if (index < 0)
                index += max;
            else if (index >= max)
                index -= max;

            return index;
        }

        private void rewind(object sender, EventArgs e)
        {
            if (button_fast_rewind.IsEnabled)
            {
                nextFields.add(field, textblock_current_pattern.Text, borderIsActive);
                Field newField = previousFields.drop();
                int lengthBefore = field.GetLength(0);
                int widthBefore = field.GetLength(1);

                field = newField.field;
                button_fast_forward.IsEnabled = true;
                button_step_forward.IsEnabled = true;
                button_delete.IsEnabled = !fieldsAreEqual(field, getEmptyField(field.GetLength(0), field.GetLength(1)));

                // No previous field is saved.
                // => disable the "back" button
                if (previousFields.firstField == null)
                {
                    button_fast_rewind.IsEnabled = false;
                    button_step_rewind.IsEnabled = false;
                }

                // field size has changed
                // => draw new field
                if (lengthBefore != field.GetLength(0) || widthBefore != field.GetLength(1))
                    drawNewField();

                textblock_current_pattern.Text = newField.name;

                printField();
            }
            else
                toggleTimer();
        }

        private void nextRound(object sender, EventArgs e)
        {
            if (field != null)
            {
                bool[,] fieldBefore = field;

                field = getFieldOfNextRound();
                updateFieldLists(fieldBefore);

                printField();
            }
        }

        private void forward(object sender, EventArgs e)
        {
            if (button_fast_forward.IsEnabled)
            {
                previousFields.add(field, textblock_current_pattern.Text, borderIsActive);
                int lengthBefore = field.GetLength(0);
                int widthBefore = field.GetLength(1);
                Field newField = nextFields.drop();

                field = newField.field;
                button_fast_rewind.IsEnabled = true;
                button_step_rewind.IsEnabled = true;
                button_delete.IsEnabled = !fieldsAreEqual(field, getEmptyField(field.GetLength(0), field.GetLength(1)));

                // No next field is saved.
                // => disable the "next" button
                if (nextFields.firstField == null)
                {
                    button_fast_forward.IsEnabled = false;
                    button_step_forward.IsEnabled = false;
                }

                // field size has changed
                // => draw new field
                if (lengthBefore != field.GetLength(0) || widthBefore != field.GetLength(1))
                    drawNewField(newField.name);

                textblock_current_pattern.Text = newField.name;

                printField();
            }
            else
                toggleTimer();
        }
        #endregion

        // Field functions
        #region
        public bool fieldsAreEqual(bool[,] field1, bool[,] field2)
        {
            if (field1 == null || field2 == null)
                return false;
            if (field1.GetLength(0) != field2.GetLength(0))
                return false;
            if (field1.GetLength(1) != field2.GetLength(1))
                return false;

            for (int i = 0; i < field1.GetLength(0); i++)
            {
                for (int j = 0; j < field1.GetLength(1); j++)
                {
                    if (field1[i, j] != field2[i, j])
                        return false;
                }
            }

            return true;
        }

        private bool[,] convertStringArrayToField(string[] input)
        {
            int length = input.Length;
            int width = input[0].Length;
            bool[,] output = new bool[length, width];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                    output[i, j] = input[i][j] == '1';
            }

            return output;
        }

        private bool[,] getEmptyField(int length, int width)
        {
            bool[,] output = new bool[length, width];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                    output[i, j] = false;
            }

            return output;
        }

        private bool[,] getRandomField(int length, int width)
        {
            Random ran = new Random();
            bool[,] field = new bool[length, width];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                    field[i, j] = ran.Next(0, 5) == 1;
            }

            return field;
        }

        private bool[,] insertSmallFieldIntoBigField(bool[,] smallField, bool[,] bigField, int x, int y)
        {
            int smallFieldLength = smallField.GetLength(0);
            int smallFieldWidth = smallField.GetLength(1);
            int bigFieldLength = bigField.GetLength(0);
            int bigFieldWidth = bigField.GetLength(1);

            if (x + smallFieldLength <= bigFieldLength && y + smallFieldWidth <= bigFieldWidth)
            {
                for (int i = 0; i < smallFieldLength; i++)
                {
                    for (int j = 0; j < smallFieldWidth; j++)
                        bigField[i + x, j + y] = smallField[i, j];
                }
            }

            return bigField;
        }

        private bool[,] parseStringArrayToField(string[] stringArray, int length, int width, int x, int y)
        {
            bool[,] smallField = convertStringArrayToField(stringArray);
            bool[,] squareField = getEmptyField(length, width);

            return insertSmallFieldIntoBigField(smallField, squareField, x, y);
        }

        private string[] convertRleToStringArray(string rleField, int length, int width)
        {
            string[] output = new string[length];
            int line = 0;
            string number = "";

            foreach (char c in rleField)
            {
                if (isNumeric(c))
                    number += c;
                else if (c == 'b' || c == 'o')
                {
                    char digit = '0';
                    if (c == 'o')
                        digit = '1';

                    if (number != "")
                    {
                        int reps = Convert.ToInt32(number);

                        for (int i = 0; i < reps; i++)
                            output[line] += digit;

                        number = "";
                    }
                    else
                        output[line] += digit;
                }
                else if (c == '$')
                {
                    if (output[line] != null && output[line].Length < width)
                    {
                        for (int i = output[line].Length; i < width; i++)
                            output[line] += '0';
                    }

                    if (number != "")
                    {
                        line += Convert.ToInt32(number);
                        number = "";
                    }
                    else
                        line++;
                }
            }

            if (output[line].Length < width)
            {
                for (int i = output[line].Length; i < width; i++)
                    output[line] += '0';
            }

            for (int i = 0; i < length; i++)
            {
                if (output[i] == null)
                    for (int j = 0; j < width; j++)
                        output[i] += '0';
            }

            return output;
        }

        private void multiplyFieldDownwards(object sender, EventArgs e)
        {
            int factor = (int)((FrameworkElement)sender).Tag;
            int length = field.GetLength(0);
            int width = field.GetLength(1);
            int newLength = length * factor;
            bool[,] multipliedField = getEmptyField(newLength, width);

            for (int i = 0; i < factor; i++)
                multipliedField = insertSmallFieldIntoBigField(field, multipliedField, i * length, 0);

            bool[,] fieldBefore = field;
            field = multipliedField;
            updateFieldLists(fieldBefore);

            string temp = textblock_current_pattern.Text;
            drawNewField();
            textblock_current_pattern.Text = temp;

            printField();
        }

        private void multiplyFieldSidewards(object sender, EventArgs e)
        {
            int factor = (int)((FrameworkElement)sender).Tag;
            int length = field.GetLength(0);
            int width = field.GetLength(1);
            int newWidth = width * factor;
            bool[,] multipliedField = getEmptyField(length, newWidth);

            for (int i = 0; i < factor; i++)
                multipliedField = insertSmallFieldIntoBigField(field, multipliedField, 0, i * width);

            bool[,] fieldBefore = field;
            field = multipliedField;
            updateFieldLists(fieldBefore);

            string temp = textblock_current_pattern.Text;
            drawNewField();
            textblock_current_pattern.Text = temp;

            printField();
        }

        private void updateFieldLists(bool[,] fieldBefore)
        {
            if (!fieldsAreEqual(fieldBefore, field) && fieldBefore != null)
            {
                previousFields.add(fieldBefore, textblock_current_pattern.Text, borderIsActive);
                if (field != null)
                {
                    button_fast_rewind.IsEnabled = true;
                    button_step_rewind.IsEnabled = true;
                }
                nextFields = new FieldList();
                button_fast_forward.IsEnabled = false;
                button_step_forward.IsEnabled = false;
            }
            else
                toggleTimer();

            button_delete.IsEnabled = !fieldsAreEqual(field, getEmptyField(field.GetLength(0), field.GetLength(1)));
        }

        private void drawNewField(string patternName = "None")
        {
            changeContentOfPatternTextblock(patternName);

            gameboard.Children.Clear();
            gameboard.RowDefinitions.Clear();
            gameboard.ColumnDefinitions.Clear();

            int numRows = field.GetLength(0);
            int numColumns = field.GetLength(1);
            double cellwidth = 0;
            rectangleArray = new Rectangle[numRows, numColumns];

            if (numRows > numColumns)
            {
                cellwidth = gameBoardLength / numRows;
                gameboard.Width = cellwidth * numColumns;
                gameboard_border.Width = gameboard.Width;
                gameboard.Height = gameBoardLength;
                gameboard_border.Height = gameBoardLength;
            }
            else
            {
                cellwidth = gameBoardWidth / numColumns;
                gameboard.Height = cellwidth * numRows;
                gameboard_border.Height = gameboard.Height;
                gameboard.Width = gameBoardWidth;
                gameboard_border.Width = gameBoardWidth;
            }

            for (int i = 0; i < numRows; i++)
            {
                RowDefinition newRow = new RowDefinition();
                newRow.Height = new GridLength(cellwidth);
                gameboard.RowDefinitions.Add(newRow);
            }

            for (int i = 0; i < numColumns; i++)
            {
                ColumnDefinition newCol = new ColumnDefinition();
                newCol.Width = new GridLength(cellwidth);
                gameboard.ColumnDefinitions.Add(newCol);
            }

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    Rectangle temp = new Rectangle();
                    temp.MouseLeftButtonDown += createCell;
                    temp.MouseRightButtonDown += killCell;
                    temp.MouseUp += continueGame;
                    temp.MouseEnter += drawCell;
                    temp.Tag = new int[] { i, j };
                    Grid.SetRow(temp, i);
                    Grid.SetColumn(temp, j);
                    gameboard.Children.Add(temp);
                    rectangleArray[i, j] = temp;
                }
            }

            button_play_pause.IsEnabled = true;
            button_step.IsEnabled = true;

            textblock_field_length.Text = numRows.ToString();
            textblock_field_width.Text = numColumns.ToString();
            updateMultiplyComboboxes();
        }

        private void printField()
        {
            int numberOfCells = 0;

            if (field != null)
            {
                for (int i = 0; i < field.GetLength(0); i++)
                {
                    for (int j = 0; j < field.GetLength(1); j++)
                    {
                        if (field[i, j])
                        {
                            rectangleArray[i, j].Fill = Brushes.Black;
                            numberOfCells++;
                        }
                        else
                            rectangleArray[i, j].Fill = Brushes.White;
                    }
                }
            }

            textblock_number_of_cells.Text = numberOfCells.ToString();
        }
        #endregion

        // Help functions
        #region
        private void newRuleSelected(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            string rules = item.Tag.ToString();
            ruleStringSurvive = rules.Split('/')[0];
            ruleStringCreate = rules.Split('/')[1];
            changeContentOfRuleTextblock(sender);
        }

        private bool isNumeric(char number)
        {
            return char.IsNumber(number);
        }

        private void toggleTimer(int index = -1)
        {
            if (index != -1)
            {
                for (int i = 0; i < timers.Length; i++)
                {
                    if (index != i)
                    {
                        timers[i].Stop();
                        if (i == 1)
                            image_play_pause.Source = path_icon_start;
                    }
                    else
                    {
                        if (timers[i].IsEnabled)
                        {
                            timers[i].Stop();
                            if (i == 1)
                                image_play_pause.Source = path_icon_start;
                        }
                        else
                        {
                            timers[i].Start();
                            if (i == 1)
                                image_play_pause.Source = path_icon_stop;
                        }
                    }
                }
            }
            else
            {
                foreach (DispatcherTimer t in timers)
                    t.Stop();
                image_play_pause.Source = path_icon_start;
            }
        }

        private void InitializeTimers(int amount)
        {
            timers = new DispatcherTimer[amount];

            for (int i = 0; i < amount; i++)
            {
                timers[i] = new DispatcherTimer();
                timers[i].Interval = new TimeSpan(0, 0, 0, 0, 1);
            }

            timers[0].Tick += rewind;
            timers[1].Tick += nextRound;
            timers[2].Tick += forward;
        }

        private void InitializeButtonImages()
        {
            string iconDirectory = Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory).ToString()).ToString() + "\\Icons\\";
            path_icon_start = new BitmapImage(new Uri("icon_play.png", UriKind.Relative));
            path_icon_stop = new BitmapImage(new Uri("icon_pause.png", UriKind.Relative));
            path_icon_border_on = new BitmapImage(new Uri("icon_border_on.png", UriKind.Relative));
            path_icon_border_off = new BitmapImage(new Uri("icon_border_off.png", UriKind.Relative));
        }

        private void InitializePatternClicks()
        {
            foreach (MenuItem item in menu_item_guns.Items)
                item.Click += newPatternClick;

            foreach (MenuItem item in menu_item_oscillators.Items)
            {
                foreach (MenuItem subItem in item.Items)
                    subItem.Click += newPatternClick;
            }

            foreach (MenuItem item in menu_item_spaceships.Items)
                foreach(MenuItem subItem in item.Items)
                    subItem.Click += newPatternClick;
        }
        #endregion

        // Patterns
        #region
        private void printNewPattern(object sender, string newField, int oldLength, int oldWidth, int newLength, int newWidth, int x, int y, bool activateBorder)
        {
            toggleTimer();

            bool[,] fieldBefore = field;

            string[] newFieldAsStringArray = convertRleToStringArray(newField, oldLength, oldWidth);
            field = parseStringArrayToField(newFieldAsStringArray, newLength, newWidth, x, y);
            updateFieldLists(fieldBefore);

            MenuItem senderAsMenuItem = (MenuItem)sender;
            drawNewField(senderAsMenuItem.Header.ToString());

            borderIsActive = activateBorder;
            setImageBorderButton();

            printField();
        }

        private void newPatternClick(object sender, EventArgs e)
        {
            object[] tag = (object[])(sender as MenuItem).Tag;
            string pattern = (string)tag[0];
            int oldLength = (int)tag[1];
            int oldWidth = (int)tag[2];
            int newLength = (int)tag[3];
            int newWidth = (int)tag[4];
            int x = (int)tag[5];
            int y = (int)tag[6];
            bool activateBorder = (bool)tag[7];
            printNewPattern(sender, pattern, oldLength, oldWidth, newLength, newWidth, x, y, activateBorder);
        }

        private void InitializePatternTags()
        {
            // Guns
            #region
            string gun;

            gun = @"7bo7bo7b2o$7b3o5b3o5b2o$10bo7bo$9b2o6b2o16b2o$30b2o2bo2bo$30bobo2b2o$3
                    3b2o$5bo28bo$5b3o26bob2o$8bo22b2obo2bo$7b2o22b2ob2o3$17bo$2b2ob2o9bobo
                    10b2o$o2bob2o8bo3bo9bo$2obo11bo3bo10b3o$3bo11bo3bo12bo$3b2o11bobo$b2o2
                    bobo9bo$o2bo2b2o$b2o16b2o$19bo$13b2o5b3o$13b2o7bo";
            gun_ak_94.Tag = new object[] { gun, 25, 38, 34, 40, 1, 1, true };

            gun = @"b2o36b$b2o17bo18b$19bobo12bobo2b$20bo12bo5b$2o7b2o23bo2bo$2obo5b2o23bo
                    bobo$3bo23bo7bo2bo$3bo23b2o7b2o$o2bo17b2o5bo10b$b2o18bo17b$21b3o15b$36
                    b2o$36b2o$b2o36b$o2bo35b$obobo16bobo4b2o5b2o2b$bo2bo17b2o4b2o5b2obo$5b
                    o12bo3bo15bo$2bobo12bobo18bo$18bo16bo2bo$36b2o";
            gun_b_52_bomber.Tag = new object[] { gun, 21, 39, 49, 45, 12, 1, false };

            gun = @"11bo38b$10b2o38b$9b2o39b$10b2o2b2o34b$38bo11b$38b2o8b2o$39b2o7b2o$10b2
                    o2b2o18b2o2b2o10b$2o7b2o39b$2o8b2o38b$11bo38b$34b2o2b2o10b$39b2o9b$38b
                    2o10b$38bo";
            gun_bi_gun.Tag = new object[] { gun, 17, 53, 70, 58, 27, 4, false };

            gun = @"24bo11b$22bobo11b$12b2o6b2o12b2o$11bo3bo4b2o12b2o$2o8bo5bo3b2o14b$2o8b
                    o3bob2o4bobo11b$10bo5bo7bo11b$11bo3bo20b$12b2o";
            gun_gosper_glider_gun.Tag = new object[] { gun, 9, 36, 24, 38, 1, 1, true };

            gun = @"25b2o5b2o$25b2o5b2o12$27b2ob2o2b$26bo5bob2$25bo7bo$25bo2bobo2bo$25b3o3
                    b3o5$17bo16b$2o15b2o15b$2o16b2o14b$13b2o2b2o15b4$13b2o2b2o15b$2o16b2o7
                    b2o5b$2o15b2o8b2o5b$17bo";
            gun_new_gun_1.Tag = new object[] { gun, 36, 35, 37, 47, 1, 1, true };

            gun = @"23b2o24b2o$23b2o24b2o$41b2o8b$40bo2bo7b$41b2o8b2$36b3o12b$36bobo12b$9b
                    2o25b3o12b$9b2o25b2o13b$8bo2bo23b3o13b$8bo2bob2o20bobo13b$8bo4b2o20b3o
                    13b$10b2ob2o36b$31b2o18b$21b2o7bo2bo17b$21b2o8b2o18b$49b2o$49b2o2$4b2o
                    18bo26b$2o4b4o10b2o2b2ob3o21b$2o2b2ob3o10b2o4b4o21b$4bo19b2o";
            gun_new_gun_2.Tag = new object[] { gun, 24, 51, 39, 53, 1, 1, true };

            gun = @"48bo$46b3o$45bo$45b2o5$43bo$42b3o$33b2o7bob2o$32bo2bo$32bobo2bo$33bo3b
                    o$37bo7bo$34bo8b3o$35b2o7bo$41bo2bo$41b3o$$38b3o$37bo2bo$37bo7b2o$36b3
                    o8bo$26b2o8bo7bo$2o23bo2bo15bo3bo$bo21bo2bobo15bo2bobo$bobo7bo10bo4bo1
                    8bo2bo$2b2o3b2o2bo3bo6bo13b2obo7b2o$6bo5bo2b3o6b3o10b3o$5b2o2bobo3b3o2
                    0bo$6b2o3bo$7b3o$29b3o$27bo3b2o$21b3o3bobo2b2o$12b3o6b3o2bo5bo7bo$16bo
                    6bo3bo2b2o6bobo$11bo4bo10bo11b2o$10bobo2bo$10bo2bo$11b2o11bo5b4o2b2o$2
                    3bobob8o2bo$23bob12o$20b2obobo11b3o$20b2ob2o11bo2bo9bo$23bo13b2o8bobo$
                    23bobo4b2o16b2o$24b2o4b2o";
            gun_period_36_glider_gun.Tag = new object[] { gun, 49, 50, 51, 53, 1, 1, true };

            gun = @"23b2o$3bo2bob2obo2bo8bo$2b2o2bo4bo2b2o5bobo$3bo2bob2obo2bo6b2o6$15bo2b
                    o$11b2o2bo2bo$11b2o2bo2bo7b2o$26b2o4b2o3bo2bo3b2o$o31b5o4b5o$3o29b2o3b
                    o2bo3b2o$3bo22b3o$2b2o$$26b3o$9b3o$33bobo$34b2o$2bo6b3o22bo$b3o$$10b2o
                    $b3o6b2o7bo2bo2b2o$19bo2bo2b2o$bobo15bo2bo$bobo$$b3o42bo$44bobo$45b2o$
                    b3o11b2o6bo2bob2obo2bo$2bo11bobo5b2o2bo4bo2b2o$14bo8bo2bob2obo2bo$13b2
                    o";
            gun_period_45_glider_gun.Tag = new object[] { gun, 38, 47, 44, 57, 3, 3, true };

            gun = @"27bo$27b4o$11bo16b4o$10bobo5b2o8bo2bo5b2o$3b2o3b2o3bo14b4o5b2o$3b2o3b2
                    o3bo4bobob2o3b4o$8b2o3bo5b2o3bo2bo$10bobo10bo$11bo8bo2bo2$26bobo$28bo$
                    24bo$26bo$25bo2$11b2o$11b2o4bo$2o6b2o6b5ob2o$2o5b3o5bo2b2o4bo$8b2o5b2o
                    8bo12bo$11b2o4bo7bo10bobo$11b2o12bo11b2o$24bo8b2o$22b2o9bobo$35bo$35b2
                    o";
            gun_period_60_glider_gun.Tag = new object[] { gun, 27, 39, 41, 57, 1, 1, true };

            gun = @"2o5b2o$2o5b2o2$4b2o$4b2o5$22b2ob2o$21bo5bo$21bo6bo2b2o$21b3o3bo3b2o$26
                    bo4$20b2o$20bo$21b3o$23bo";
            gun_simkin_glider_gun.Tag = new object[] { gun, 21, 33, 39, 35, 17, 1, true };

            gun = @"18b2o25b$19bo7bo17b$19bobo14b2o7b$20b2o12b2o2bo6b$24b3o7b2ob2o6b$24b2o
                    b2o7b3o6b$24bo2b2o12b2o2b$25b2o14bobo$35bo7bo$43b2o2$2o23bo19b$bo21bob
                    o19b$bobo13b3o4b2o19b$2b2o3bo8bo3bo24b$6bob2o6bo4bo23b$5bo4bo6b2obo9bo
                    14b$6bo3bo8bo3b2o6bo13b$7b3o13bobo3b3o13b$25bo19b$25b2o";
            gun_true_period_22_gun.Tag = new object[] { gun, 21, 45, 47, 59, 1, 1, true };

            gun = @"23bo2bo$21b6o$17b2obo8bo$13b2obobobob8o2bo$11b3ob2o3bobo7b3o$10bo4b3o2
                    bo3bo3b2o$11b3o3b2ob4obo3bob2o$12bobo3bo5bo4bo2bo4b2obo$10bo8bob2o2b2o
                    2b2o5bob2obo$10b5ob4obo4b3o7bo4bo$15b2o4bo4bob3o2b2obob2ob2o$12b5ob3o4
                    b2ob2o3bobobobobo$11bo5b2o4b2obob2o5bo5bo$12b5o6b2obo3bo3bobob2ob2o$2o
                    b2o9b2o2bo5bobo4bo2b3obobo$bobobobob2o3b3obo6bo2bobo4b3o2bo$o2bo7bo6b2
                    o3b3o8bobob2o$3o2bo4b2o11bo10bo$5b4obo17b2o4b2o$2b2obo6bo14bo3bo2b2o$b
                    o4bo3bo16bo6b2o$b3obo4bo16bo3bo2bo$11bo2bo3bo9b2o4bobob2o$b3obo4bo8b2o
                    3bo10b3o2bo$bo4bo3bo7bo6bo8b3obobo$2b2obo6bo10b3o8bobob2ob2o$5b4obo24b
                    o5bo$3o2bo4b2o21bobobobobo$o2bo7bo9b2o10b2obob2ob2o$bobobobob2o10bo8bo
                    5bo4bo$2ob2o17b3o6bo4bob2obo$24bo4b3o5b2obo";
            gun_true_period_24_gun.Tag = new object[] { gun, 32, 43, 45, 45, 1, 1, true };

            gun = @"b2o23b2o21b$b2o23bo22b$24bobo22b$15b2o7b2o23b$2o13bobo31b$2o13bob2o30b
                    $16b2o31b$16bo32b$44b2o3b$16bo27b2o3b$16b2o31b$2o13bob2o13bo3bo12b$2o1
                    3bobo13bo5bo7b2o2b$15b2o14bo13b2o2b$31b2o3bo12b$b2o30b3o13b$b2o46b$33b
                    3o13b$31b2o3bo12b$31bo13b2o2b$31bo5bo7b2o2b$32bo3bo12b2$44b2o3b$44b2o3
                    b5$37b2o10b$37bobo7b2o$39bo7b2o$37b3o9b$22bobo24b$21b3o25b$21b3o25b$21
                    bo15b3o9b$25bobo11bo9b$21b2o4bo9bobo9b$16b2o4bo3b2o9b2o10b$15bobo6bo24
                    b$15bo33b$14b2o";
            gun_vacuum.Tag = new object[] { gun, 43, 49, 65, 54, 1, 1, true };
            #endregion

            // Oscillators
            #region
            string oscillator;

            // Unnamed
            #region
            oscillator = @"16bo12bo16b$9b2o24b2o9b$8b3o3b2o14b2o3b3o8b$14b2ob2o8b2ob2o14b$16bo12b
                           o16b4$2bo40bo2b$b2o40b2o$b2o40b2ob4$2b2o38b2o2b$2b2o38b2o2b$o3bo36bo3b
                           o$3bo38bo3b$3bo38bo3b9$3bo38bo3b$3bo38bo3b$o3bo36bo3bo$2b2o38b2o2b$2b2
                           o38b2o2b4$b2o40b2o$b2o40b2o$2bo40bo2b4$16bo12bo16b$14b2ob2o8b2ob2o14b$
                           8b3o3b2o14b2o3b3o8b$9b2o24b2o9b$16bo12bo";
            oscillator_104_p_177.Tag = new object[] { oscillator, 46, 46, 64, 64, 9, 9, false };

            oscillator = @"15b2o3b2o15b3$6b2o21b2o6b$6b2o21b2o6b2$3b2o27b2o3b$3b2o9b2o5b2o9b2o3b$
                           9b3ob2o7b2ob3o9b$8bobo15bobo8b$8b2o17b2o8b$8bo19bo8b2$8bo19bo8b$7b2o19
                           b2o7b$o6bo21bo6bo$o35bo4$o35bo$o6bo21bo6bo$7b2o19b2o7b$8bo19bo8b2$8bo1
                           9bo8b$8b2o17b2o8b$8bobo15bobo8b$9b3ob2o7b2ob3o9b$3b2o9b2o5b2o9b2o3b$3b
                           2o27b2o3b2$6b2o21b2o6b$6b2o21b2o6b3$15b2o3b2o";
            oscillator_112_p_51.Tag = new object[] { oscillator, 37, 37, 43, 43, 3, 3, false };

            oscillator = @"37b2o$8b2o27bo$9bo25bobo$9bobo23b2o2b$10b2o13bobo2bobo6b$14bobo2bobo2b
                           o2bo2bo2bo5b$13bo2bo2bo2bo2bobo2bobo6b$14bobo2bobo13b2o2b$10b2o23bobo$
                           9bobo25bo$9bo27b2o$8b2o29b$23b2o14b$19b2o2b2o14b$18b2o19b$19bo19b$14b2
                           o23b$14b2o23b$29b2o8b$2o27bo9b$bo25bobo9b$bobo23b2o10b$2b2o13bobo2bobo
                           14b$6bobo2bobo2bo2bo2bo2bo13b$5bo2bo2bo2bo2bobo2bobo14b$6bobo2bobo13b2
                           o10b$2b2o23bobo9b$bobo25bo9b$bo27b2o8b$2o";
            oscillator_117_p_18.Tag = new object[] { oscillator, 30, 39, 32, 41, 1, 1, false };

            oscillator = @"11b2o11b2o11b$11b2o11b2o11b3$6bo23bo6b$5bobo5bo9bo5bobo5b$4bo2bo5bob2o
                           3b2obo5bo2bo4b$5b2o10bobo10b2o5b$15bobobobo15b$16bo3bo16b2$2o33b2o$2o3
                           3b2o$5b2o23b2o5b2$6bobo19bobo6b$6bo2bo17bo2bo6b$7b2o19b2o7b2$7b2o19b2o
                           7b$6bo2bo17bo2bo6b$6bobo19bobo6b2$5b2o23b2o5b$2o33b2o$2o33b2o2$16bo3bo
                           16b$15bobobobo15b$5b2o10bobo10b2o5b$4bo2bo5bob2o3b2obo5bo2bo4b$5bobo5b
                           o9bo5bobo5b$6bo23bo6b3$11b2o11b2o11b$11b2o11b2o";
            oscillator_124_p_37.Tag = new object[] { oscillator, 37, 37, 39, 39, 1, 1, false };

            oscillator = @"18bo$18b3o$21bo$20b2o2$32b2o$32b2o$26bobo$28bo2$22b3o$15b2o5bo2bo$15b2
                           o5bo3bo$5b2o19bo$5b2o15bo3bo$22bo2bo8b2o$22b3o9b2o2$7b2o36b2o$45bo$7bo
                           4b3o28bobo$11bo3bo27b2o$10bo5bo13b3ob3o$10bo5bo13bo5bo$10b3ob3o13bo5bo
                           $2b2o27bo3bo$bobo28b3o4bo$bo$2o36b2o2$11b2o9b3o$11b2o8bo2bo$20bo3bo15b
                           2o$20bo19b2o$20bo3bo5b2o$21bo2bo5b2o$22b3o2$18bo$18bobo$13b2o$13b2o2$2
                           5b2o$25bo$26b3o$28bo";
            oscillator_132_p_37.Tag = new object[] { oscillator, 47, 47, 49, 49, 1, 1, false };

            oscillator = @"4b2o10b2o7b$3bo2bo2b4o2bo2bo6b$3b3o2b6o2b3o6b$6b10o9b$5bo10bo8b$5b2o8b
                           2o8b2$11bo7bo5b$5bo6bo5b2o5b$5b2o3bo3bo10b$16b3o2bobo$bobo2b3obo3bo3bo
                           b4o$b4obo4b3o8b2o$b2o22b3$11b3o11b5$5bo19b$4o3bo11bo5b$3bo3bo9bo3b4o$7
                           bo3b3o3bo3bo3b$4bo7bo4bo7b$5b2o13bo4b$18b2o5b2$5b2o8b2o8b$5bo2b6o2bo8b
                           $6b2o6b2o9b$3b3o10b3o6b$3bo2bobo4bobo2bo6b$4b2o4b2o4b2o";
            oscillator_144_p_24.Tag = new object[] { oscillator, 35, 25, 37, 27, 1, 1, false };

            oscillator = @"22bo10bo24b$21bobo8bobo23b$22bo10bo24b2$3ob3o12bo5bo4bo5bo12b3ob3o2b$3
                           bo6bo3bo4bo5bo4bo5bo4bo3bo6bo5b$2o3b2o3bob3o3bo7bo2bo7bo3b3obo3b2o3b2o
                           2b$3bo6bo3bo4bo5bo4bo5bo4bo3bo6bo5b$3ob3o12bo5bo4bo5bo12b3ob3o2b2$22bo
                           35b$20b3o9b3o23b$19bo7b3o7b2o19b$19b2o7b3o7bo19b$23b3o9b3o20b$35bo22b2
                           $2b3ob3o12bo5bo4bo5bo12b3ob3o$5bo6bo3bo4bo5bo4bo5bo4bo3bo6bo3b$2b2o3b2
                           o3bob3o3bo7bo2bo7bo3b3obo3b2o3b2o$5bo6bo3bo4bo5bo4bo5bo4bo3bo6bo3b$2b3
                           ob3o12bo5bo4bo5bo12b3ob3o2$24bo10bo22b$23bobo8bobo21b$24bo10bo";
            oscillator_186_p_24.Tag = new object[] { oscillator, 26, 58, 28, 60, 1, 1, false };

            oscillator = @"2o12b$bo12b$bobo10b$2b2o10b$8bo5b$7b2o5b$8bo5b5$5bo8b$5b2o7b$5bo8b$10b
                           2o2b$10bobo$12bo$12b2o";
            oscillator_22_p_36.Tag = new object[] { oscillator, 18, 14, 20, 16, 1, 1, false };

            oscillator = @"2o8b2o5b$2o7bo7b$12bo4b$8b2obo5b$8b2o7b5$7b2o8b$5bob2o8b$4bo12b$7bo7b2
                           o$5b2o8b2o";
            oscillator_26_p_40.Tag = new object[] { oscillator, 14, 17, 22, 19, 4, 1, false };

            oscillator = @"4b3o9b2$6bo9b$6b2o8b$5bo10b$8bo7b$4bo3bo7b$9b2o5b$2o2b3o5bo3b$2o5bo4b2
                           obo$7bo3bo3bo$7bobo5bo3$6b2o8b$6b2o";
            oscillator_34_p_13.Tag = new object[] { oscillator, 16, 16, 19, 19, 2, 1, false };

            oscillator = @"2bo14b$2b3o12b$5bo9b2o$4b2o9bo$13bobo$8b2o3b2o2b$7bo9b$6bo10b$7bobo7b$
                           7bo9b2$2b2o13b$bobo13b$bo9b2o4b$2o9bo5b$12b3o2b$14bo";
            oscillator_35_p_52.Tag = new object[] { oscillator, 17, 17, 19, 19, 1, 1, false };

            oscillator = @"2o25b$bo25b$bobo13b3o7b$2b2o3bo8bo3bo6b$6bob2o6bo4bo5b$5bo4bo6b2obo6b$
                           6bo3bo8bo3b2o2b$7b3o13bobo$25bo$25b2o";
            oscillator_36_p_22.Tag = new object[] { oscillator, 10, 27, 16, 29, 3, 1, false };

            oscillator = @"11bo18b$9b2obo17b$9b2obo17b$10b2o18b2$7b2o12b2o7b$7b2o12b2o7b3$21bobo6
                           b$4b2o18bo5b$2obo2bob2o10bo4bo2b2o$o4bo2b2o10b2obo2bob2o$4bo19b2o4b$bo
                           bo";
            oscillator_47_p_72.Tag = new object[] { oscillator, 15, 30, 21, 32, 5, 1, false };

            oscillator = @"7b2obo2bob2o7b$2o4bo2bo4bo2bo4b2o$2o5bobo4bobo5b2o$8bo6bo8b6$8bo6bo8b$
                           2o5bobo4bobo5b2o$2o4bo2bo4bo2bo4b2o$7b2obo2bob2o";
            oscillator_48_p_31.Tag = new object[] { oscillator, 13, 24, 23, 26, 5, 1, false };

            oscillator = @"19bo7b$17b3o7b$16bo10b$16b2o9b$2o19b2o4b$bo19bo5b$bobo15bobo5b$2b2o15b
                           2o6b$14bo12b$13bobo11b$12b2ob2o10b7$6b2o15b2o2b$5bobo15bobo$5bo19bo$4b
                           2o19b2o$9b2o16b$10bo16b$7b3o17b$7bo";
            oscillator_49_p_88.Tag = new object[] { oscillator, 25, 27, 27, 29, 1, 1, false };

            oscillator = @"31b2o$31bo$29bobo$29b2o2b$26bo6b$26bo6b$17bobo6bo6b$16bo3b2o7b2o2b$16b
                           o3bo8bobo$16bo14bo$2o29b2o$bo14bo16b$bobo8bo3bo16b$2b2o7b2o3bo16b$6bo6
                           bobo17b$6bo26b$6bo26b$2b2o29b$bobo29b$bo31b$2o";
            oscillator_50_p_35.Tag = new object[] { oscillator, 21, 33, 23, 35, 1, 1, false };

            oscillator = @"15b2o13b$15b2o13b5$13b2o15b$14b2o14b$10bo4bo14b$10bo3bo15b$10b2o7b3o8b
                           $19bo10b2$2o21bo6b$2o5b2o11bob2o6b$6b2obo11b2o5b2o$6bo21b2o2$10bo19b$8
                           b3o7b2o10b$15bo3bo10b$14bo4bo10b$14b2o14b$15b2o13b5$13b2o15b$13b2o";
            oscillator_56_p_27.Tag = new object[] { oscillator, 30, 30, 32, 32, 1, 1, false };

            oscillator = @"21bo$21bo$20bobo$21bo$21bo$21bo$21bo$20bobo$21bo$21bo5$2bo2bo4bo2bo$3o
                           2b6o2b3o$2bo2bo4bo2bo$22b2o$21b2o$23bo$32bo4bo$30b2ob4ob2o$32bo4bo";
            oscillator_6_bits.Tag = new object[] { oscillator, 23, 40, 31, 45, 4, 1, false };

            oscillator = @"bo4b2o$obo2bobo$bo4bo5$12b2o$8b2obo2bo$7bobo5b5o$7bo3bo5bo2bo$7bo3b2o8
                           bo$8bo8b2o3bo$9bo2bo5bo3bo$10b5o5bobo$15bo2bob2o$16b2o5$23bo4bo$22bobo
                           2bobo$22b2o4bo";
            oscillator_60_p_33.Tag = new object[] { oscillator, 24, 30, 26, 32, 1, 1, false };

            oscillator = @"5bo19bo5b$5b3o15b3o5b$8bo13bo8b$7b2o13b2o7b2$2o27b2o$bo11b2o14bo$bobo9
                           b2o12bobo$2b2o23b2o2b$19bobo9b$18bo2bo9b$18bo2bo9b$11bo7bo11b$9bo2bo18
                           b$9bo2bo18b$9bobo19b$2b2o23b2o2b$bobo12b2o9bobo$bo14b2o11bo$2o27b2o2$7
                           b2o13b2o7b$8bo13bo8b$5b3o15b3o5b$5bo19bo";
            oscillator_78_p_70.Tag = new object[] { oscillator, 25, 31, 27, 33, 1, 1, false };

            oscillator = @"10b2o12b2o10b$10b2o12b2o10b5$12b2o8b2o12b$11bo2bo6bo2bo11b$11bo12bo11b
                           $11bo12bo11b$2o10bo10bo10b2o$2o5b3o16b3o5b2o$6bo3bo14bo3bo6b$6bo22bo6b
                           $7bo20bo7b7$7bo20bo7b$6bo22bo6b$6bo3bo14bo3bo6b$2o5b3o16b3o5b2o$2o10bo
                           10bo10b2o$11bo12bo11b$11bo12bo11b$11bo2bo6bo2bo11b$12b2o8b2o12b5$10b2o
                           12b2o10b$10b2o12b2o";
            oscillator_88_p_28.Tag = new object[] { oscillator, 36, 36, 38, 38, 1, 1, false };

            oscillator = @"21bo12b$4bo2bo3b5o2b3obo11b$4bo2bo7bo2bo2bo12b$bob2o2b5o3bo2bo15b$obo3
                           1b$bo32b7$6bobo3b4o18b$6bo2bo2bo2bo18b$6bo5b3o10b2o7b$7b2o10b3o5bo6b$1
                           8bo2bo2bo2bo6b$18b4o3bobo6b7$32bo$31bobo$15bo2bo3b5o2b2obo$12bo2bo2bo7
                           bo2bo4b$11bob3o2b5o3bo2bo4b$12bo";
            oscillator_92_p_33_1.Tag = new object[] { oscillator, 30, 34, 34, 36, 2, 1, false };

            oscillator = @"9b2o4b2o7b2o12b$9bobo2bobo7b2o12b$11bo2bo23b$10bo4bo22b$10b2o2b2o22b$1
                           2b2o24b2$36b2o$2o24bo6b2o2bo$o2b2o19b2obo5bob2o$b2obo19bo2bo4bo5b$5bo5
                           b3o10b3o5bo5b$5bo4bo2bo19bob2o$b2obo5bob2o19b2o2bo$o2b2o6bo24b2o$2o36b
                           2$24b2o12b$22b2o2b2o10b$22bo4bo10b$23bo2bo11b$12b2o7bobo2bobo9b$12b2o7
                           b2o4b2o";
            oscillator_98_p_25.Tag = new object[] { oscillator, 23, 38, 25, 40, 1, 1, false };
            #endregion

            // A
            #region
            oscillator = @"10b2o10b$10b2o10b$5bo10bo5b$4bobo8bobo4b$3bobo3bo2bo3bobo3b$2bobo4bo2b
                           o4bobo2b$3bo5bo2bo5bo3b3$4b3o8b3o4b$2o18b2o$2o18b2o$4b3o8b3o4b3$3bo5bo
                           2bo5bo3b$2bobo4bo2bo4bobo2b$3bobo3bo2bo3bobo3b$4bobo8bobo4b$5bo10bo5b$
                           10b2o10b$10b2o";
            oscillator_achims_p_11.Tag = new object[] { oscillator, 22, 22, 24, 24, 1, 1, false };

            oscillator = @"2o24b2o$2o24b2o$18b2o8b$17bo2bo7b$18b2o8b$14bo13b$13bobo12b$12bo3bo11b
                           $12bo2bo12b2$12bo2bo12b$11bo3bo12b$12bobo13b$13bo14b$8b2o18b$7bo2bo17b
                           $8b2o18b$2o24b2o$2o24b2o";
            oscillator_achims_p_144.Tag = new object[] { oscillator, 19, 28, 21, 30, 1, 1, true };

            oscillator = @"7b2o4b$7bobo3b$2bo4bob2o2b$b2o5bo4b$o2bo9b$3o10b2$10b3o$9bo2bo$4bo5b2o
                           b$2b2obo4bo2b$3bobo7b$4b2o";
            oscillator_achims_p_16.Tag = new object[] { oscillator, 13, 13, 17, 17, 2, 2, false };

            oscillator = "4bo4b$2o2bobo2b$obo6b$7b2o$bo7b$o6bo$2obobo3b$5bo";
            oscillator_almosymmetric.Tag = new object[] { oscillator, 8, 9, 10, 11, 1, 1, false };
            #endregion

            // B
            #region
            oscillator = @"2o9b2o10b$4obo5b2o10b$obo2b3o15b$11bo11b$4b2o4bobo10b$4bo5bo2bo4bo4b$1
                           1b2o4b2o4b2$15b3o2bobo$10b2o5bob4o$10b2o9b2o";
            oscillator_bakers_dozen.Tag = new object[] { oscillator, 11, 23, 13, 27, 1, 2, false };

            oscillator = @"4b2o6b2o4b$3bo2bo4bo2bo3b$3bobo6bobo3b$b2o2b3o2b3o2b2o$o6bo2bo6bo$ob2o
                           10b2obo$bobo10bobo$3b2o8b2o3b3$3b2o8b2o3b$bobo10bobo$ob2o10b2obo$o6bo2
                           bo6bo$b2o2b3o2b3o2b2o$3bobo6bobo3b$3bo2bo4bo2bo3b$4b2o6b2o";
            oscillator_bottle.Tag = new object[] { oscillator, 18, 18, 20, 20, 1, 1, false };

            oscillator = "9bo$7bobo$6bobo$2o3bo2bo$2o4bobo$7bobo9b2o$9bo9bobo$21bo$21b2o";
            oscillator_buckar2o.Tag = new object[] { oscillator, 9, 23, 11, 25, 1, 1, false };

            oscillator = "3bo2b$bobo2b$5bo$5o$5bo$bobo2b$3bo";
            oscillator_by_flops.Tag = new object[] { oscillator, 7, 6, 9, 8, 1, 1, false };
            #endregion

            // C
            #region
            oscillator = @"33bo3bo$2o3b2o26b5o$bobobo3bo2bo6b2o3bo2bo7bo2b$b2ob2o2b2o3b2o4b2o2b2o
                           3b2o4bobo$bobobo3bo2bo6b2o3bo2bo7bo2b$2o3b2o26b5o$33bo3bo";
            oscillator_carnival_shuttle.Tag = new object[] { oscillator, 7, 38, 9, 41, 1, 1, false };

            oscillator = @"2o48b2o$bo48bo$bobo21b2o21bobo$2b2o8bo12b2o12b2o7b2o2b$11b2o26bobo10b$
                           10b2o29bo10b$11b2o2b2o22b3o10b4$11b2o2b2o22b3o10b$10b2o29bo10b$11b2o26
                           bobo10b$2b2o8bo12b2o12b2o7b2o2b$bobo21b2o21bobo$bo48bo$2o48b2o";
            oscillator_centinal.Tag = new object[] { oscillator, 17, 52, 19, 54, 1, 1, false };

            oscillator = "2bo$obo$bobo$bo";
            oscillator_clock.Tag = new object[] { oscillator, 4, 4, 6, 6, 1, 1, false };

            oscillator = "2b4o2b$2bo2bo2b$3o2b3o$o6bo$o6bo$3o2b3o$2bo2bo2b$2b4o";
            oscillator_cross.Tag = new object[] { oscillator, 8, 8, 12, 12, 2, 2, false };

            oscillator = @"3bo2bobo2bo3b$3bo2bobo2bo3b$2b2o2bobo2b2o2b$3o4bo4b3o$7bo7b2$3o9b3o$3b
                           2o5b2o3b$3o9b3o2$7bo7b$3o4bo4b3o$2b2o2bobo2b2o2b$3bo2bobo2bo3b$3bo2bob
                           o2bo";
            oscillator_cross_2.Tag = new object[] { oscillator, 15, 15, 17, 17, 1, 1, false };
            #endregion

            // D
            #region
            oscillator = @"6bo6b$5bobo5b$4bobobo4b$4bo3bo4b$2b2o2bo2b2o2b$bo4bo4bo$obob2ob2obobo$
                           bo4bo4bo$2b2o2bo2b2o2b$4bo3bo4b$4bobobo4b$5bobo5b$6bo";
            oscillator_diamond_ring.Tag = new object[] { oscillator, 13, 13, 15, 15, 1, 1, false };

            oscillator = @"bo11b$b3o7b2o$4bo6bo$3b2o4bobo$9b2o2b$6bo6b$4b2obo5b2$2bo3bo2bo3b$bob2
                           o4bo3b$bo6bo4b$2o7b3o$11bo";
            oscillator_dinner_table.Tag = new object[] { oscillator, 13, 13, 15, 15, 1, 1, false };

            oscillator = @"5b2o16b2o4b$6bo16bo5b$6bobo12bobo5b$7b2o12b2o6b2$4b2o18b2o3b$4bobo10b2
                           o4bobo3b$5bo10bobo5bo4b$2bo13b2o9bo$2b6o8bo5b6o$7bo14bo6b$4b2o18b2o3b$
                           4bo20bo3b$5bo18bo4b$2b3o2bo14bo2b3o$2bo2b3o8bo5b3o3bo$3bo12b2o7b3ob$4b
                           2o10bobo5bo4b$6bo10b2o4bo2b2o$4b2o18b2obo$bo2bo20bo3b$obobo2b2o12b2o2b
                           o3b$bo2bobobo12bobob2o2b$4bobo16bo2bo2b$5b2o16b2o";
            oscillator_diuresis.Tag = new object[] { oscillator, 25, 29, 27, 31, 1, 1, false };
            #endregion

            // E
            #region
            oscillator = @"bo14bo$obo4bo7bobo$bo3b2ob2o6bo$7bo10b4$7bo10b$bo3b2ob2o6bo$obo4bo7bob
                           o$bo14bo";
            oscillator_eureka.Tag = new object[] { oscillator, 11, 18, 17, 20, 3, 1, false };

            oscillator = @"bo14bo3b$obo4bo7bobo2b$bo3b2ob2o6bo3b$7bo12b4$9bo10b$3bo3b2ob2o6bo$2bo
                           bo4bo7bobo$3bo14bo";
            oscillator_eureka_v_2.Tag = new object[] { oscillator, 11, 20, 17, 22, 3, 1, false };
            #endregion

            // F
            #region
            oscillator = "2o4b$2obo2b$4bo$bo4b$2bob2o$4b2o";
            oscillator_figure_eight.Tag = new object[] { oscillator, 6, 6, 12, 12, 3, 3, false };

            oscillator = @"9bo9b2$3b2obo5bob2o3b$3bo5bo5bo3b$4b2ob2ob2ob2o4b2$6b2o3b2o6b$2o15b2o$
                           o2bo3bobobo3bo2bo$b3ob9ob3o$4bo4bo4bo4b$3b2o9b2o3b$3bo11bo3b$5bo7bo5b$
                           4b2o7b2o";
            oscillator_fountain.Tag = new object[] { oscillator, 15, 19, 17, 21, 1, 1, false };
            #endregion

            // G
            #region
            oscillator = @"8b3o10b$7bo2bo10b$7bo2bo10b$7b2o12b4$17b3o$17bo2bo$20bo$3o15b3o$o20b$o
                           2bo17b$b3o17b4$12b2o7b$10bo2bo7b$10bo2bo7b$10b3o";
            oscillator_gabriels_p_138.Tag = new object[] { oscillator, 21, 21, 35, 35, 7, 7, false };

            oscillator = @"10b2o8b$10bo9b$4b2ob2obo4b2o3b$2bo2bobobo5bo4b$2b2o4bo8bo2b$16b2o2b2$1
                           6b2o2b$o10bo3bobo2b$3o7b3o3bo3b$3bo6bobo4b3o$2bobo14bo$2b2o16b2$2b2o16
                           b$2bo8bo4b2o2b$4bo5bobobo2bo2b$3b2o4bob2ob2o4b$9bo10b$8b2o";
            oscillator_gourmet.Tag = new object[] { oscillator, 20, 20, 22, 22, 1, 1, false };
            #endregion

            // H
            #region
            oscillator = @"5b2o3b2o5b$5bobobobo5b$6bo3bo6b2$5b2o3b2o5b$2o2bobo3bobo2b2o$obob2o5b2
                           obobo$bo13bob2$bo13bo$obob2o5b2obobo$2o2bobo3bobo2b2o$5b2o3b2o5b2$6bo3
                           bo6b$5bobobobo5b$5b2o3b2o";
            oscillator_harbor.Tag = new object[] { oscillator, 17, 17, 19, 19, 1, 1, false };

            oscillator = @"22b2o15b$22b2o15b11$9bo10b2o3b2o12b$7bobo12b3o14b$6bobo12bo3bo13b$2o3b
                           o2bo13bobo14b$2o4bobo14bo15b$7bobo6bobo20b$9bo6b2o21b$17bo3bo17b$21b2o
                           6bo9b$20bobo6bobo7b$15bo14bobo4b2o$14bobo13bo2bo3b2o$13bo3bo12bobo6b$1
                           4b3o12bobo7b$12b2o3b2o10bo9b11$15b2o22b$15b2o";
            oscillator_hectic.Tag = new object[] { oscillator, 39, 39, 41, 41, 1, 1, false };

            oscillator = @"2o$bo$bobo9b2o$2bobo8bo$11bobo$11b2o$5b2o$5b2ob2o$8b2o$2b2o$bobo$bo8bo
                           bo$2o9bobo$13bo$13b2o";
            oscillator_honey_thieves.Tag = new object[] { oscillator, 15, 15, 17, 17, 1, 1, false };
            #endregion

            // J
            #region
            oscillator = @"8bob2ob2obo$8b2obobob2o2$8b3o3b3o$7bo3bobo3bo$7bobobobobobo$11bobo$4b2
                           o2b3o3b3o2b2o$2obo3bo9bo3bob2o$bobobobo9bobobobo$o2bo3bo9bo3bo2bo$2o2b
                           3o11b3o2b2o2$2o2b3o11b3o2b2o$o2bo3bo9bo3bo2bo$bobobobo9bobobobo$2obo3b
                           o9bo3bob2o$4b2o2b3o3b3o2b2o$11bobo$7bobobobobobo$7bo3bobo3bo$8b3o3b3o2
                           $8b2obobob2o$8bob2ob2obo";
            oscillator_jasons_p_11.Tag = new object[] { oscillator, 25, 25, 27, 27, 1, 1, false };

            oscillator = @"b2o6b2o2b$o2bo4bo2bo$o2bo4bo2bo$o2bo4bo2bo$b2o6b2o2b3$7bo5b$5bo2bob2o$
                           6b2o2b2ob3$6b4o3b$5b6o2b$4b8o$3b2o6b2o$4b8o$5b6o2b$6b4o";
            oscillator_jolson.Tag = new object[] { oscillator, 19, 13, 24, 20, 3, 3, false };

            oscillator = @"2o16b2o2b$bo16bo3b$bob2o10b2obo3b$2bo3bo6bo3bo4b$7bo4bo9b$3bo3bo4bo3bo
                           5b$7bo4bo9b$2bo3bo6bo3bo4b$bob2o10b2obo3b$bo16bo3b$2o9bo6b2o2b$9bo2bob
                           2o6b$10b2o2b2o6b$2b2o16b2o$3bo16bo$3bobo4b4o4bobo$4b2o3b6o3b2o2b$8b8o6
                           b$7b2o6b2o5b$8b8o6b$4b2o3b6o3b2o2b$3bobo4b4o4bobo$3bo16bo$2b2o16b2o";
            oscillator_jolson_period_9.Tag = new object[] { oscillator, 24, 22, 26, 24, 1, 1, false };

            oscillator = @"26b2o$2o23bo2bo$bo21bo2bobo$bobo18bo4bo$2b2o18bo$13b3o8b3o$13bo$13bobo
                           $14b2o$23b2o$23bobo$25bo$12b3o8b3o$16bo18b2o$11bo4bo18bobo$10bobo2bo21
                           bo$10bo2bo23b2o$11b2o";
            oscillator_js_p_36.Tag = new object[] { oscillator, 18, 39, 20, 41, 1, 1, false };
            #endregion

            // K
            #region
            oscillator = "2bo4bo2b$2b6o2b$2bo4bo2b4$2b6o2b$bo6bo$o8bo$bo6bo$2b6o";
            oscillator_karels_p_15.Tag = new object[] { oscillator, 11, 10, 16, 18, 2, 4, false };

            oscillator = "2bo2bobo$2obob3o$bo6bo$2o5bob2$bo5b2o$o6bo$b3obob2o$bobo2bo";
            oscillator_koks_galaxy.Tag = new object[] { oscillator, 9, 9, 15, 15, 3, 3, false };
            #endregion

            // L
            #region
            oscillator = @"61bo$59b3o$56bobo3b2o$54b3ob4o2bob2o$53bo7bob2obo$52bob5obo2bo2bo2bo$1
                           4b2o36b2o3bob2o2bobob3o$14b2o34b2o3bo8bobo$49bobob2obo8bo2b2o$14b4o3b2
                           o26bo2bobobo5bo3b3obo$14bo3bo2bo28bobo2b2o4bobo6bo$17bobobo29bo2b2o6bo
                           5b2ob2o$17b3obobo22b2o6b3o9b2obobo$19bob2obo21bobo5b2o10bo2bobo$6b2ob2
                           o7bo5bo23bo2b2o14bob2o$6b2obo7b2ob5ob2o17bo2b5o13b2o$9bo16b2o17b3o4bo7
                           bob3o2bob2o$9bob2o2bo4bob3o5bo2bo2bo2bo2bo5bo2bo7b4o2bobobo$10bobob2o3
                           bo5b23obo9b3ob2o2bobo$11b3o4bo6bo25bo12bobobo$15bobo4bo3bo2b3o2b3o2b3o
                           2b5o2b2o3b3o3b2o2b2o$9b5obo6bo3b2o3b2o3b2o3b2o4bo4b2obob2o2bo2b2o$9bo3
                           bobobo2b2o2b3o2b3o2b3o2b3o2b5o8bo4bo2bo$12bo2bobo6bo29bo2bo5b2o$13b3ob
                           o4b2ob23o2bobo2bob3o$18b2o2bobo2bo2bo2bo2bo2bo2bo2bo2bob3obobo3bo$15b2
                           obob3obo23bobobobobo2b2o$15b2obo2bo2b2o21b2obobobob2o$18bo5bo23bobobob
                           o$18bobobobo23bo5bo$17b2obobob2o21b2o2bo2b2o$18bob3obo23bob3obo$18bo2b
                           o2bo23bobobobo$17b2o5b2o21b2obobob2o$18bobobobo23bo5bo$18bobobobo23bo2
                           bo2bo$17b2ob3ob2o21b2ob3ob2o$18bo2bo2bo23bobobobo$18bo5bo23bobobobo$17
                           b2obobob2o21b2o5b2o$18bobobobo23bo2bo2bo$18bob3obo23bob3obo$17b2o2bo2b
                           2o21b2obobob2o$18bo5bo23bobobobo$18bobobobo23bo5bo$15b2obobobob2o21b2o
                           2bo2bob2o$12b2o2bobobobobo23bob3obob2o$12bo3bobob3obo2bo2bo2bo2bo2bo2b
                           o2bo2bobo2b2o$13b3obo2bobo2b23ob2o4bob3o$8b2o5bo2bo29bo6bobo2bo$7bo2bo
                           4bo8b5o2b3o2b3o2b3o2b3o2b2o2bobobo3bo$7b2o2bo2b2obob2o4bo4b2o3b2o3b2o3
                           b2o3bo6bob5o$5b2o2b2o3b3o3b2o2b5o2b3o2b3o2b3o2bo3bo4bobo$4bobobo12bo25
                           bo6bo4b3o$3bobo2b2ob3o9bob23o5bo3b2obobo$3bobobo2b4o7bo2bo5bo2bo2bo2bo
                           2bo5b3obo4bo2b2obo$2b2obo2b3obo7bo4b3o17b2o16bo$5b2o13b5o2bo17b2ob5ob2
                           o7bob2o$2b2obo14b2o2bo23bo5bo7b2ob2o$bobo2bo10b2o5bobo21bob2obo$bobob2
                           o9b3o6b2o22bobob3o$2ob2o5bo6b2o2bo29bobobo$2bo6bobo4b2o2bobo28bo2bo3bo
                           $2bob3o3bo5bobobo2bo26b2o3b4o$3b2o2bo8bob2obobo$6bobo8bo3b2o34b2o$3b3o
                           bobo2b2obo3b2o36b2o$3bo2bo2bo2bob5obo$6bob2obo7bo$5b2obo2b4ob3o$9b2o3b
                           obo$11b3o$11bo";
            oscillator_light_speed_oscillator_3.Tag = new object[] { oscillator, 73, 73, 75, 75, 1, 1, false };

            oscillator = @"16bo$15b3o3$15b3o2$15bobo$15bobo2$15b3o3$15b3o$16bo2$bo2bob2obo2bo15b2
                           o$2o2bo4bo2b2o3b2o7bo4bo$bo2bob2obo2bo3bo2bo5bo6bo$16bobo5bo8bo$17bo6b
                           o8bo$24bo8bo$25bo6bo$26bo4bo$28b2o$18b3o$17bo3bo$16bo5bo2$15bo7bo$15bo
                           7bo2$16bo5bo$17bo3bo$18b3o";
            oscillator_loaflipflop.Tag = new object[] { oscillator, 34, 34, 40, 40, 2, 2, false };
            #endregion

            // M
            #region
            oscillator = @"11b2o$12bo$12bobo$10b2obobo$9bobobobo$8bobo2bo2b2o$7bo5bo4bo$6bo7b4o$5
                           bo12b3o$4bo5bo5b2o2bo$3bobo3bobo3bobo$o2b2o5bo5bo$3o12bo$3b4o7bo$2bo4b
                           o5bo$3b2o2bo2bobo$5bobobobo$5bobob2o$6bobo$8bo$8b2o";
            oscillator_mm_p_11.Tag = new object[] { oscillator, 21, 21, 23, 23, 1, 1, false };

            oscillator = "2o3b2o$bobobo$b2ob2o$bobobo$2o3b2o";
            oscillator_monogram.Tag = new object[] { oscillator, 5, 7, 9, 9, 2, 1, false };
            #endregion

            // N
            #region
            oscillator = @"19bo2b2obo3bo19b$17b3o2bob2o3b3o17b$7b2o7bo15bo7b2o7b$8bo7b2o5b3o5b2o7
                           bo8b$8bobo11bo3bo11bobo8b$9b2o11b2ob2o11b2o9b2$2bo10b3o17b3o10bo2b$2b3
                           o9bo19bo9b3o2b$5bo13b3o5b3o13bo5b$4b2o14bo7bo14b2o4b3$7bo33bo7b$7b2o31
                           b2o7b$7bo33bo7b$2b2o18b2ob2o18b2o2b$bobo18bo3bo18bobo$bo21b3o21bo$2o7b
                           o15bobo11bo7b2o$9b2o15b2o10b2o9b$9bo9b2o18bo9b$4b2o10b2o2bo10b2o10b2o2
                           b2o$2obobo10bob2o10bobo10bobo2bo$bobo14bo11bo14bobo$o2bobo10bobo10b2ob
                           o10bobob2o$2o2b2o10b2o10bo2b2o10b2o4b$9bo18b2o9bo9b$9b2o10b2o15b2o9b$2
                           o7bo11bobo15bo7b2o$bo21b3o21bo$bobo18bo3bo18bobo$2b2o18b2ob2o18b2o2b$7
                           bo33bo7b$7b2o31b2o7b$7bo33bo7b3$4b2o14bo7bo14b2o4b$5bo13b3o5b3o13bo5b$
                           2b3o9bo19bo9b3o2b$2bo10b3o17b3o10bo2b2$9b2o11b2ob2o11b2o9b$8bobo11bo3b
                           o11bobo8b$8bo7b2o5b3o5b2o7bo8b$7b2o7bo15bo7b2o7b$17b3o3b2obo2b3o17b$19
                           bo3bob2o2bo";
            oscillator_newshuttle.Tag = new object[] { oscillator, 49, 49, 51, 51, 1, 1, false };
            #endregion

            // O
            #region
            oscillator = "3b2o3b$2bo2bo2b$bo4bo$o6bo$o6bo$bo4bo$2bo2bo2b$3b2o";
            oscillator_octagon_2.Tag = new object[] { oscillator, 8, 8, 10, 10, 1, 1, false };

            oscillator = @"7b2o$7b2o2$6b4o$5bo4bo$4bo6bo$3bo8bo$2obo8bob2o$2obo8bob2o$3bo8bo$4bo6
                           bo$5bo4bo$6b4o2$7b2o$7b2o";
            oscillator_octagon_4.Tag = new object[] { oscillator, 16, 16, 18, 18, 1, 1, false };
            #endregion

            // P
            #region
            oscillator = @"7b2o$7bobo$9bo3$5bo3bo5b3o$5bo2bo4b3o$5b2o7bo6b2o$6b4o4bobo5bo$6bo2b2o
                           2b2o2bo2b2o$13bo2$9bo$b2o2bo2b2o2b2o2bo$o5bobo4b4o$2o6bo7b2o$7b3o4bo2b
                           o$5b3o5bo3bo3$13bo$13bobo$14b2o";
            oscillator_p_11_pinwheel.Tag = new object[] { oscillator, 23, 23, 25, 25, 1, 1, false };

            oscillator = @"15b2o4b2o4b2o4b$14bo2bobo4bobo2bo3b$14b3o10b3o3b$17b2o6b2o6b$16bo2b6o2
                           bo5b$16b2o8b2o5b2$13bo19b$8b4o3bo11bo5b$11bo3bo4bo4bo3b4o$15bo3b3o3bo3
                           bo3b$12bo12bo7b$13b2o13bo4b$26b2o5b2$30b2o$30bo2b$28bobo2b$28b2o3b$13b
                           3o17b$13bobo17b$3b2o8b3o17b$2bobo28b$2bo30b$b2o30b2$5b2o26b$4bo7bo5b2o
                           13b$7bo3b3o6bo12b$3bo3bo2b5o2bo15b$4o3bo2b5o2bo3bo11b$5bo6bo4bo3b4o8b$
                           19bo13b$10b2o21b$5b2obo4bob2o16b$5bo10bo16b$6b2o6b2o17b$3b3o2b6o2b3o14
                           b$3bo2bo8bo2bo14b$4b2o10b2o";
            oscillator_p_156_hans_leo_hassler.Tag = new object[] { oscillator, 40, 33, 42, 35, 1, 1, false };

            oscillator = @"26bo$3bo20b3o$3b3o17bo$6bo16b2o$5b2o21b2o$28bo$8b2o16bobo$8bo17b2o$20b
                           3o$7b3o$2b2o17bo$bobo16b2o$bo$2o21b2o$5b2o16bo$6bo17b3o$3b3o20bo$3bo";
            oscillator_p_18_honey_farm_hassler.Tag = new object[] { oscillator, 18, 30, 20, 32, 1, 1, false };

            oscillator = @"22b2o7bo$23bo8bo2bo$23bobo4bo4bo4b2o$24b2o13bo2bo$29bo8b2ob2o$28b2ob2o
                           8bo$28bo2bo13b2o$29b2o4bo4bo4bobo$35bo2bo8bo$39bo7b2o4$23bo8b2o$16bo5b
                           2o7bobo$15bobo3bo10bo$15b2o5bobo$22bobo2$19b2o$2o16bo2bo$bo16b2obo$bob
                           o4b3o5bo2b2o$2b2o4bobo4bo$7bo3bo3bo3bo$7bo3bo3bo3bo$11bo4bobo4b2o$6b2o
                                      2bo5b3o4bobo$5bob2o16bo$5bo2bo16b2o$6b2o";
            oscillator_p_22_lumps_of_muck_hassler.Tag = new object[] { oscillator, 31, 49, 36, 51, 3, 1, false };

            oscillator = @"19b2o$19bobo$22bo2b2o$20b2obo2bo$19bobob2o$20bo4$22b2o$21bo2bo$21bobo$
                           21bobo$16b2o4bo$15bobo18bo$15bo20b3o$14b2o23bo$38b2o$19bobo$19b2o12b2o
                           13bo2b2o$20bo11b2o13bobo2bo$34bo5b3o5b2obo$39bo3bo6bo$40b2obo4b2o$42bo
                           5bo$50bo$2b2o45b2o$2bo$4bo5bo$3b2o4bob2o$2bo6bo3bo$bob2o5b3o5bo$o2bobo
                           13b2o11bo$2o2bo13b2o12b2o$31bobo$13b2o$13bo23b2o$14b3o20bo$16bo18bobo$
                           30bo4b2o$29bobo$29bobo$28bo2bo$29b2o4$32bo$28b2obobo$26bo2bob2o$26b2o2
                           bo$31bobo$32b2o";
            oscillator_p_26_glider_shuttle.Tag = new object[] { oscillator, 53, 53, 55, 55, 1, 1, false };

            oscillator = @"10bo$4b2o3bobo3b2o$3bobo4bo4bobo$3bo13bo$2ob2o11b2ob2o$2obobo9bobob2o$
                           3bob11obo$3bobob7obobo$4bo4bobo4bo3$6bo7bo$5bo9bo$5b2o3bo3b2o$10bo$10b
                           o2$4bo3bobobo3bo$3bobo9bobo$3bobo9bobo$2obobob7obobob2o$2obo13bob2o$3b
                           o13bo$3bobo4bo4bobo$4b2o3bobo3b2o$10bo";
            oscillator_p_32_blinker_hassler.Tag = new object[] { oscillator, 26, 21, 28, 23, 1, 1, false };

            oscillator = @"9b2o7b2o$9bo2b2ob2o2bo$10b2obobob2o$11bobobobo$11bob3obo$12b5o4$13b3o$
                           6b2o6bo$5bobo6bo$5b2o2$9b3o6b3o$9bobo6b3o$9b3o6b3o$21b3o$21b3o$21b3o2$
                           4bo3bobobo3bo$3bobo9bobo$3bobo9bobo$2obobob7obobob2o$2obo13bob2o$3bo13
                           bo$3bobo4bo4bobo$4b2o3bobo3b2o$10bo";
            oscillator_p_32_blinker_hassler_2.Tag = new object[] { oscillator, 30, 24, 32, 28, 1, 1, false };

            oscillator = @"2o16b2o$bo16bo$bobo12bobo$2b2o12b2o2b$7bo4bo7b$5b2ob4ob2o5b$7bo4bo7b$2
                           b2o12b2o2b$bobo12bobo$bo16bo$2o16b2o$11bo8b$9bo2bo7b$9bo2bo7b$10bo9b$2
                           o16b2o$bo16bo$bobo12bobo$2b2o12b2o2b$7bo4bo7b$5b2ob4ob2o5b$7bo4bo7b$2b
                           2o12b2o2b$bobo12bobo$bo16bo$2o16b2o";
            oscillator_p_36_toad_hassler.Tag = new object[] { oscillator, 26, 20, 28, 22, 1, 1, false };

            oscillator = @"b2o20b2o$o2bo19b2o$o25b$o25b$ob2o20b2o$2b2o18bob2o$22bo3b$22bo3b$b2o13
                           b2o4bo2bo$b2o12bo2bo4b2o$15bo2bo7b$14b2ob2o7b$15b2o9b2$7bo10bo7b$6bobo
                           8bobo6b$7bo4b2o4bo7b$10bo4bo10b$10bo4bo10b$10bo4bo10b$11bo2bo11b$9bobo
                           2bobo9b$9b2o4b2o";
            oscillator_p_40_b_heptomino_shuttle.Tag = new object[] { oscillator, 23, 26, 25, 30, 1, 2, false };

            oscillator = @"22b2o8b$21bo2bo7b$10bo13bo7b$9bobo12bo7b$10bo7bo3bobo7b$17bo3bobo8b$8b
                           5o4bo4bo9b$7bo4bo5b4o4b2o4b$6bob2o16b2o2b2o$3bo2bobo15b2obo2b2o$2bobob
                           o9bo7b3o5b$3bo2bo4b3o3b2o13b$6b2o3b2o3b2o14b$11bo13b2o5b$25b2o5b2$10bo
                           bo19b$5b2o4b2o19b$4bo2bo3bo7b2o11b$7bo10bo2bo2b2o6b$7bo10bobo4bo2bo3b$
                           bo3bobo11bo5bobobo2b$o3bobo15b2obo2bo3b$o4bo16bo2bo6b$b4o4b2o8bo4bo7b$
                           9b2o2b2o4b5o8b$7b2obo2b2o17b$7b3o11bo10b$20bobo9b$21bo10b$8b2o22b$8b2o";
            oscillator_p_42_glider_shuttle.Tag = new object[] { oscillator, 32, 32, 34, 34, 1, 1, false };

            oscillator = @"9b2o4b2o4b2o8b$8bo2bobo4bobo2bo7b$8b3o10b3o7b$11b2o6b2o10b$10bo2b6o2bo
                           9b$10b2o8b2o9b12$2o12b3o12b2o$2o11bo3bo11b2o$13b2ob2o13b5$13b2ob2o13b$
                           2o11bo3bo11b2o$2o12b3o12b2o12$10b2o8b2o9b$10bo2b6o2bo9b$11b2o6b2o10b$8
                           b3o10b3o7b$8bo2bobo4bobo2bo7b$9b2o4b2o4b2o";
            oscillator_p_44_pi_heptomino_hassler.Tag = new object[] { oscillator, 44, 31, 46, 33, 1, 1, false };

            oscillator = @"3b2o9b2o2b2o9b2o5b$4bo9bo4bo9bo6b$3bo11bo2bo11bo5b$3b2o9b2o2b2o9b2o5b$
                           4bo4bo4bo4bo4bo4bo6b$b3ob9ob4ob9ob3o3b$o2bo3bobobo3bo2bo3bobobo3bo2bo2
                           b$2o30b2o2b$6b2o3b2o8b2o3b2o8b2$4b2ob2ob2ob2o4b2ob2ob2ob2o6b$3bo5bo5bo
                           2bo5bo5bo5b$3b2obo5bob2o2b2obo5bob2o5b2$9bo14bo11b2$16b2o18b$10bo4bo2b
                           o4bo12b$10bo5b2o5bo12b$9bobo10bobo11b$10bo12bo12b$10bo12bo12b$27b2o7b$
                           4b3o19bobo7b$4b3o19b2o8b$4b3o11bo13b3o$b3o12bo2bo12b3o$b3o12bo2bo12b3o
                           $b3o13bo11b3o4b$8b2o19b3o4b$7bobo19b3o4b$7b2o27b$12bo12bo10b$12bo12bo1
                           0b$11bobo10bobo9b$12bo5b2o5bo10b$12bo4bo2bo4bo10b$18b2o16b2$11bo14bo9b
                           2$5b2obo5bob2o2b2obo5bob2o3b$5bo5bo5bo2bo5bo5bo3b$6b2ob2ob2ob2o4b2ob2o
                           b2ob2o4b2$8b2o3b2o8b2o3b2o6b$2b2o30b2o$2bo2bo3bobobo3bo2bo3bobobo3bo2b
                           o$3b3ob9ob4ob9ob3o$6bo4bo4bo4bo4bo4bo4b$5b2o9b2o2b2o9b2o3b$5bo11bo2bo1
                           1bo3b$6bo9bo4bo9bo4b$5b2o9b2o2b2o9b2o";
            oscillator_p_48_toad_hassler.Tag = new object[] { oscillator, 54, 36, 56, 40, 1, 2, false };

            oscillator = @"21b2o42b$20bo2bo41b$17bo3bobo41b$15b6obo42b$14bo5bo44b$14bob2obo2bo42b
                           $15bo3bob2o42b$17bobo45b$16b2o3b2o42b$15bo2b2obo43b$13bo2bo3bo2bo9bo31
                           b$13b2obo3bob2o7b3o31b$14bobob2obo8bo34b$14bob3o2bo8b2o33b$15bo3bob2o4
                           2b$16b4o2bo5bo36b$20bo6bobo35b$16b4o2bo5b2o2b2o31b$16bo2bob2o9b2o31b$2
                           1bo43b$19bobo31b2o10b$19b2o6bo23b3o5b2o4b$25b2o23bo4bo2bo2bo3b$26b2o18
                           b2obob4obo2bobo3b$10b2o5b2o28bobobo4b2obob2o2b$11bo5b2o28bobob2o2bo5bo
                           3b$11bobo29b2ob2ob2obo2bob3obo3b$12b2o29bo4bo4b2o5b2obo$16b2o26b3o3b3o
                           2b2obo3bobo$15bobo20b2o6b2ob2o2bo2bob2obo2bo$16bo20b2o14b2o7b2o$39bo25
                           b2$25bo39b$b2o7b2o14b2o20bo16b$o2bob2obo2bo2b2ob2o6b2o20bobo15b$obo3bo
                           b2o2b3o3b3o26b2o16b$bob2o5b2o4bo4bo29b2o12b$3bob3obo2bob2ob2ob2o29bobo
                           11b$3bo5bo2b2obobo28b2o5bo11b$2b2obob2o4bobobo28b2o5b2o10b$3bobo2bob4o
                           bob2o18b2o26b$3bo2bo2bo4bo23b2o25b$4b2o5b3o23bo6b2o19b$10b2o31bobo19b$
                           43bo21b$31b2o9b2obo2bo16b$31b2o2b2o5bo2b4o16b$35bobo6bo20b$36bo5bo2b4o
                           16b$42b2obo3bo15b$33b2o8bo2b3obo14b$34bo8bob2obobo14b$31b3o7b2obo3bob2
                           o13b$31bo9bo2bo3bo2bo13b$43bob2o2bo15b$42b2o3b2o16b$45bobo17b$42b2obo3
                           bo15b$42bo2bob2obo14b$44bo5bo14b$42bob6o15b$41bobo3bo17b$41bo2bo20b$42
                           b2o";
            oscillator_p_49_glider_shuttle.Tag = new object[] { oscillator, 65, 65, 67, 67, 1, 1, false };

            oscillator = @"16bo16b$15bobo15b$10bo4bobo15b$9bobo2b2ob2o2b2o10b$10bo3bo5bo2bo9b$15b
                           3o4bo2bo7b$8b5o4bo2bobob2o7b$7bo4bo5b2o2bo3b2o5b$6bo2bo11b2o3bo6b$3bo2
                           bob2o18bo4b$2bobobo5bo11b4obo3b$3bo2bo4bobo4bo5bo4bo3b$6b2o2bo2bo2bobo
                           7bobo4b$11b2o4b2o6bo7b$3b2o20bo3bo3b$b3obo20b2ob3o$o4bo6bo14bo4bo$b3ob
                           2o6bo13bob3o$3bo3bo3b3o14b2o3b$7bo12b2o11b$4bobo12bo2bo2b2o6b$3bo4bo10
                           bobo4bo2bo3b$3bob4o11bo5bobobo2b$4bo18b2obo2bo3b$6bo3b2o11bo2bo6b$5b2o
                           3bo2b2o5bo4bo7b$7b2obobo2bo4b5o8b$7bo2bo4b3o15b$9bo2bo5bo3bo10b$10b2o2
                           b2ob2o2bobo9b$15bobo4bo10b$15bobo15b$16bo";
            oscillator_p_50_glider_shuttle.Tag = new object[] { oscillator, 33, 33, 35, 35, 1, 1, false };

            oscillator = @"2o44b2o$o2b3o36b3o2bo$b2o30b3o9b2o$6bo18bo15bo6b$6bo3bo12b2ob2o3bo5bo3
                           bo6b$b2o5b2ob2o12bo5bo5bo7b2o$o2b3o4bo20bo5bo4b3o2bo$2o44b2o$33b3o12b2
                           $6bo2bo28bo2bo6b$5bob2obo26bob2obo5b$6bo2bo28bo2bo6b$6bo2bo28bo2bo6b$5
                           bob2obo26bob2obo5b$6bo2bo28bo2bo";
            oscillator_p_50_traffic_jam.Tag = new object[] { oscillator, 16, 48, 20, 50, 2, 1, false };

            oscillator = @"2o25b2o$bo25bo$bobo7bo13bobo$2b2o5bo2bo5bo6b2o2b$12bo5b2o9b$8bo10b2o8b
                           $8bo3b2o4b2o9b$9b5o15b2$9b5o15b$8bo3b2o4b2o9b$8bo10b2o8b$12bo5b2o9b$2b
                           2o5bo2bo5bo6b2o2b$bobo7bo13bobo$bo25bo$2o25b2o";
            oscillator_p_54_shuttle.Tag = new object[] { oscillator, 17, 29, 19, 31, 1, 1, false };

            oscillator = @"23b2o$23b2ob3$6b2o16b2o$6b2o14bob2o$22bo3b$22bo3b$22bo2bo$6bo16b2o$5bo
                           b2o17b$b2o2bo2bo17b$b2o5bo17b3$2b2o14b2o6b$ob2o14b2o6b$o25b$o25b$o2bo2
                           2b$b2o";
            oscillator_p_56_b_heptomino_shuttle.Tag = new object[] { oscillator, 21, 26, 23, 30, 1, 2, false };
            oscillator = @"15bo32bo18b$13b3o32b3o16b$12bo38bo15b$12b2o36b2o15b2$bo60bo4b$obo4bo48
                           bo4bobo3b$bo5b2o6b2o30b2o6b2o5bo4b$7bo7b2o30b2o7bo10b3$19b2o22b2o22b$7
                           bo11b2o5b2o8b2o5b2o11bo10b$bo5b2o17bo10bo17b2o5bo4b$obo4bo16bobo10bobo
                           16bo4bobo3b$bo22b2o12b2o22bo4b4$14bo5bo5bo2bob2obo2bo5bo5bo17b$13b3o3b
                           3o3b2o2bo4bo2b2o3b3o3b3o16b$26bo2bob2obo2bo29b4$13bo7bo20bo7bo16b$12bo
                           bo5bobo10bo7bobo5bobo15b$13bo7bo10bo2bo6bo7bo16b$16bo7bo7bo2bo9bo7bo13
                           b$15bobo5bobo8bo9bobo5bobo12b$16bo7bo20bo7bo13b4$29bo2bob2obo2bo26b$16
                           b3o3b3o3b2o2bo4bo2b2o3b3o3b3o13b$17bo5bo5bo2bob2obo2bo5bo5bo14b4$4bo22
                           b2o12b2o22bo$3bobo4bo16bobo10bobo16bo4bobo$4bo5b2o17bo10bo17b2o5bo$10b
                           o11b2o5b2o8b2o5b2o11bo7b$22b2o22b2o19b3$10bo7b2o30b2o7bo7b$4bo5b2o6b2o
                           30b2o6b2o5bo$3bobo4bo48bo4bobo$4bo60bob2$15b2o36b2o12b$15bo38bo12b$16b
                           3o32b3o13b$18bo32bo";
            oscillator_p_58_toadsucker.Tag = new object[] { oscillator, 56, 67, 58, 69, 1, 1, false };

            oscillator = @"10b2o10b2o$9bo2bo8bo2bo$9b3o2b6o2b3o$12b2o6b2o$11bo10bo$11b2obo4bob2o$
                           16b2o2$16bo$16bo$16bo$13bo$2bo4bo3bo2bo3b3o5bo4bo$2b6o3bobo12b6o$2bo4b
                           o4bo3bo9bo4bo$16bo$16bo2$2b6o18b6o$bo6bo16bo6bo$o8bo14bo8bo$bo6bo16bo6
                           bo$2b6o18b6o";
            oscillator_p_60_hassler.Tag = new object[] { oscillator, 23, 34, 27, 42, 1, 4, false };

            oscillator = "2bo4bo2b$2ob4ob2o$2bo4bo";
            oscillator_pentadecathlon.Tag = new object[] { oscillator, 3, 10, 11, 18, 4, 4, false };

            oscillator = @"13bo$3b2o8b3o$3b2o11bo7b2o$15b2o7bo$b6o15bobo$o6bo14b2o$2o2b3o21b2o$28
                           bobo$4bo20b2obobo$4bo2bo17bobobo$o3bo22bo$o3bo20bo2bo$o3bo3bo8b2o9bo$4
                           bo12bobo4bo3bo3bo$4bo2bo11bo8bo3bo$5bo11bobo8bo3bo$3bobobo9b2o6bo2bo$2
                           bobob2o20bo$2bobo$3b2o21b3o2b2o$9b2o14bo6bo$8bobo15b6o$8bo7b2o$7b2o7bo
                           11b2o$17b3o8b2o$19bo";
            oscillator_period_48_pi_hassler.Tag = new object[] { oscillator, 26, 33, 28, 37, 1, 2, false };

            oscillator = "3bo4b$3bobo2b$bo6b$6b2o$2o6b$6bo$2bobo3b$4bo";
            oscillator_phoenix_1.Tag = new object[] { oscillator, 8, 8, 10, 10, 1, 1, false };

            oscillator = @"14b2o10b2o31b$13bo2bo8bo2bo30b$13b3o2b6o2b3o30b$16b2o6b2o33b$15bo10bo3
                           2b$15b2obo4bob2o32b$20b2o37b$12bo25b2o10bo8b$7b4o3bo4bo6bo11b2o10bo8b$
                           10bo3bo2b5o2bo3b4o18bo8b$14bo2b5o2bo3bo20b2o8b$11bo6b3o3bo22bo11b$12b2
                           o5bo7bo18bo4bo7b$25b2o19bo9b2o$48b3o5bobo$53b2obobo$53bobobo$55bo3b$35
                           b2o11bo5bo2bo$7b2o26bobo10b2o4bo4b$7b2o28b2o9bo5bo3bo$35bobo16bo3bo$20
                           b2o13b2o17bo4b$23bobo28bo2bo$25bo21b3o5bo3b$23bo21bo7bobobo$19bo25bo4b
                           o2b2obobo$9bo10b2o24bo9bobo$9bo38b2o6b2o$9bo39bo9b$b2o6b2o38bo9b$obo9b
                           o36bo9b$obob2o2bo4bo45b$bobobo7bo45b$3bo5b3o47b$bo2bo54b$4bo54b$o3bo54
                           b$o3bo5bo39b2o7b$4bo4b2o39b2o7b$bo2bo5bo48b$3bo55b$bobobo53b$obob2o53b
                           $obo5b3o48b$b2o9bo19b2o25b$7bo4bo18bo7bo5b2o12b$11bo22bo3b3o6bo11b$8b2
                           o20bo3bo2b5o2bo14b$8bo18b4o3bo2b5o2bo3bo10b$8bo10b2o11bo6bo4bo3b4o7b$8
                           bo10b2o25bo12b$37b2o20b$32b2obo4bob2o15b$32bo10bo15b$33b2o6b2o16b$30b3
                           o2b6o2b3o13b$30bo2bo8bo2bo13b$31b2o10b2o";
            oscillator_pi_orbital.Tag = new object[] { oscillator, 59, 59, 61, 61, 1, 1, false };

            oscillator = @"11b2o11b$6b2obo4bob2o6b$6bo10bo6b$7b2o6b2o7b$4b3o2b6o2b3o4b$4bo2bo8bo2
                           bo4b$b2obobo10bobob2o$bobobo12bobobo$3bo16bo3b$bo2bo14bo2bo$4bo7b3o4bo
                           4b$o3bo7bobo4bo3bo$o3bo7bobo4bo3bo$4bo14bo4b$bo2bo14bo2bo$3bo16bo3b$bo
                           bobo12bobobo$b2obobo10bobob2o$4bo2bo8bo2bo4b$4b3o2b6o2b3o4b$7b2o6b2o7b
                           $6bo10bo6b$6b2obo4bob2o6b$11b2o";
            oscillator_pi_portraitor.Tag = new object[] { oscillator, 24, 24, 26, 26, 1, 1, false };

            oscillator = @"21bo10b$20bobo9b2$13b2o3bo3b2o8b$13b2o2bo5bo8b$18bobo11b$20b2o10b$14b2
                           o16b$3b2o8bo2bo15b$bobo9bobo4bo11b$o5bo7bo5bo11b$bo3b2o2b3o8bo11b2$3bo
                           bo16b2o3b2o3b$4bo12bo3bo2bo2b2o3b$8bo7b3o3bobo7b$7bobo6bobo4bo8b$3b2o2
                           bo2bo16bo4b$3b2o3b2o16bobo3b2$11bo8b3o2b2o3bo$11bo13bo5bo$11bo5b2o9bob
                           o$16bobo8b2o3b$16b3o13b$10b2o20b$11bobo18b$8bo5bo2b2o13b$8b2o3bo3b2o13
                           b2$9bobo20b$10bo";
            oscillator_popover.Tag = new object[] { oscillator, 32, 32, 34, 34, 1, 1, false };

            oscillator = @"8bo25bo8b$7bobo23bobo7b$8bo25bo8b$13bo15bo13b$6b5o2b3o11b3o2b5o6b$5bo4
                           bo5bo9bo5bo4bo5b$4bo2bo7b2o9b2o7bo2bo4b$bo2bob2o27b2obo2bo$obobo5bo21b
                           o5bobobo$bo2bo4bobo19bobo4bo2bo$4b2o2bo2bo6bo5bo6bo2bo2b2o4b$9b2o6b3o3
                           b3o6b2o9b4$9b2o21b2o9b$4b2o2bo2bo19bo2bo2b2o4b$bo2bo4bobo19bobo4bo2bo$
                           obobo5bo21bo5bobobo$bo2bob2o27b2obo2bo$4bo2bo27bo2bo4b$5bo4bo3b2o11b2o
                           3bo4bo5b$6b5o4bo11bo4b5o6b$13bobobo7bobobo13b$8bo3bobob2o7b2obobo3bo8b
                           $7bobo2bobo13bobo2bobo7b$8bo4b2o13b2o4bo";
            oscillator_pre_pulsar_hassler_55.Tag = new object[] { oscillator, 27, 43, 29, 45, 1, 1, false };

            oscillator = @"16bo3bo16b$10b2o4bo3bo4b2o10b$10bo5bo3bo5bo10b$7b2obo15bob2o7b$6bobob2
                           o13b2obobo6b$6bobo6bo5bo6bobo6b$4b2o2bo5b3o3b3o5bo2b2o4b$3bo4b2o17b2o4
                           bo3b$3b5o21b5o3b$7bo21bo7b$b4o27b4o$bo2bo27bo2bob2$15b2o3b2o15b$6bo9bo
                           3bo9bo6b$5b2o6bo9bo6b2o5b$3o3bo6b2o7b2o6bo3b3o4$3o3bo6b2o7b2o6bo3b3o$5
                           b2o6bo9bo6b2o5b$6bo9bo3bo9bo6b$15b2o3b2o15b2$bo2bo27bo2bo$b4o27b4o$7bo
                           21bo7b$3b5o21b5o3b$3bo4b2o17b2o4bo3b$4b2o2bo5b3o3b3o5bo2b2o4b$6bobo6bo
                           5bo6bobo6b$6bobob2o13b2obobo6b$7b2obo15bob2o7b$10bo5bo3bo5bo10b$10b2o4
                           bo3bo4b2o10b$16bo3bo";
            oscillator_pre_pulsar_shuttle_26.Tag = new object[] { oscillator, 37, 37, 39, 39, 1, 1, false };

            oscillator = @"15bo12b$13b3o12b$12bo15b$12b2o14b2$bo26b$obo4bo20b$bo5b2o6b2o11b$7bo7b
                           2o11b3$19b2o7b$7bo11b2o5b2o$bo5b2o17bo$obo4bo16bobo$bo22b2o2b4$14bo5bo
                           7b$13b3o3b3o6b5$13bo7bo6b$12bobo5bobo5b$13bo7bo";
            oscillator_pre_pulsar_shuttle_29.Tag = new object[] { oscillator, 28, 28, 30, 30, 1, 1, false };

            oscillator = @"11bo7bo11b$10bobo5bobo10b$11bo7bo11b4$6bo17bo6b$6bo17bo6b$6bo17bo6b$2b
                           2o23b2o2b$bobo7b3o3b3o7bobo$bo8bo3bobo3bo8bo$2o27b2o2$7b2o13b2o7b$7b2o
                           13b2o7b2$2o27b2o$bo8bo3bobo3bo8bo$bobo7b3o3b3o7bobo$2b2o23b2o2b$6bo17b
                           o6b$6bo17bo6b$6bo17bo6b4$11bo7bo11b$10bobo5bobo10b$11bo7bo";
            oscillator_pre_pulsar_shuttle_47.Tag = new object[] { oscillator, 30, 31, 32, 33, 1, 1, false };

            oscillator = @"2b2obo13bob2o$2bob2o13b2obo2$3b3o13b3o$2bo2bo13bo2bo$2b2o17b2o2$2b4o13
                           b4o$2bo2bo13bo2bo$3bo5bo5bo5bo$3o5b3o3b3o5b3o$o23bo4$o23bo$3o19b3o$3bo
                           17bo$2bo2bo13bo2bo$2b4o13b4o2$2b2o17b2o$2bo2bo13bo2bo$3b3o13b3o2$2bob2
                           o13b2obo$2b2obo13bob2o";
            oscillator_pre_pulsar_shuttle_58.Tag = new object[] { oscillator, 27, 25, 29, 27, 1, 1, false };

            oscillator = @"2b3o3b3o2b2$o4bobo4bo$o4bobo4bo$o4bobo4bo$2b3o3b3o2b2$2b3o3b3o2b$o4bob
                           o4bo$o4bobo4bo$o4bobo4bo2$2b3o3b3o";
            oscillator_pulsar.Tag = new object[] { oscillator, 15, 15, 17, 17, 2, 2, false };
            #endregion

            // Q
            #region
            oscillator = "2o2b2o$o2bobo$bo4b$4bo$obo2bo$2o2b2o";
            oscillator_quad.Tag = new object[] { oscillator, 6, 6, 8, 8, 1, 1, false };

            oscillator = @"10b3o3b3o10b2$8bo4bobo4bo8b$8bo4bobo4bo8b$8bo4bobo4bo8b$10b3o3b3o10b2$
                               8b3o7b3o8b$2b3o2bo4bo3bo4bo2b3o2b$7bo4bo3bo4bo7b$o4bobo4bo3bo4bobo4bo$
                               o4bo17bo4bo$o4bo2b3o7b3o2bo4bo$2b3o19b3o2b2$2b3o19b3o2b$o4bo2b3o7b3o2b
                               o4bo$o4bo17bo4bo$o4bobo4bo3bo4bobo4bo$7bo4bo3bo4bo7b$2b3o2bo4bo3bo4bo2
                               b3o2b$8b3o7b3o8b2$10b3o3b3o10b$8bo4bobo4bo8b$8bo4bobo4bo8b$8bo4bobo4bo
                               8b2$10b3o3b3o";
            oscillator_quasar.Tag = new object[] { oscillator, 29, 29, 33, 33, 2, 2, false };

            oscillator = "9bo12b$7bobo12b$6bobo13b$2o3bo2bo11b2o$2o4bobo11b2o$7bobo12b$9bo";
            oscillator_queen_bee_shuttle.Tag = new object[] { oscillator, 7, 22, 9, 24, 1, 1, false };
            #endregion

            // R
            #region
            oscillator = @"o12bo$3o4bo3b3o$3bobobo2bo3b$2bo6bobo2b$2bobo6bo2b$3bo2bobobo3b$3o3bo4
                                      b3o$o12bo";
            oscillator_revolver.Tag = new object[] { oscillator, 8, 14, 10, 16, 1, 1, false };

            oscillator = @"16bo17b$14bobobo15b$12bobobobobo13b$10bobobobobobobo11b$8bobobo2b2obob
                           obobo9b$6bobobobo6bo2bobobo7b$4bobobo2bo10bobobobo5b$5b2obo14bo2bobobo
                           3b$3bo3bo18bob2o4b$4b3o20bo3bo2b$2bo25b3o3b$3b2o27bo$bo3bo24b2o2b$2b4o
                           23bo3bo$o29b3o$b3o29bo$o3bo23b4o2b$2b2o24bo3bo$bo27b2o3b$3b3o25bo2b$2b
                           o3bo20b3o4b$4b2obo18bo3bo3b$3bobobo2bo14bob2o5b$5bobobobo10bo2bobobo4b
                           $7bobobo2bo6bobobobo6b$9bobobobob2o2bobobo8b$11bobobobobobobo10b$13bob
                           obobobo12b$15bobobo14b$17bo";
            oscillator_ring_of_fire.Tag = new object[] { oscillator, 30, 34, 32, 36, 1, 1, false };

            oscillator = @"bo12b$b3o8b2o$4bo7bo$3b2o5bobo$10b2o2b2$6b2o6b$5b2obo5b$6b3o5b$2b2o3b3
                           o4b$bobo5b2o3b$bo7bo4b$2o8b3o$12bo";
            oscillator_roteightor.Tag = new object[] { oscillator, 14, 14, 16, 16, 1, 1, false };
            #endregion

            // S
            #region
            oscillator = @"8bo11bo8b$7bobo9bobo7b$8bo11bo8b2$6b5o7b5o6b$5bo4bo7bo4bo5b$4bo2bo13bo
                           2bo4b$bo2bob2o13b2obo2bo$obobo5bo7b2o4bobobo$bo2bo4bobo5bo2bo3bo2bo$4b
                           2o2bo2bo6bobo2b2o4b$9b2o8bo9b$13b2o14b$13bobo13b$14bo14b$17b3o9b$5bobo
                           2bo5bo12b$5b3obob2o3bo3bo8b$4bo6bo4bo2bobo7b$5bo5b2o5bobo2bo5b$19bo3bo
                           5b$4b2o5bo11bo5b$5bo6bo7b3o6b$4b2obob3o17b$6bo2bobo";
            oscillator_sailboat.Tag = new object[] { oscillator, 25, 29, 29, 31, 1, 1, false };

            oscillator = @"13b2o9b$12bo2bo8b$13bobo8b$9b2o3bo9b$10bob2o10b$12bo11b$12bo11b2$2o8b3
                           o6bo4b$4obo8bo3b2o4b$obo2b3obo4bo9b$9bo4bob3o2bobo$4b2o3bo8bob4o$4bo6b
                           3o8b2o2$11bo12b$11bo12b$10b2obo10b$9bo3b2o9b$8bobo13b$8bo2bo12b$9b2o";
            oscillator_skewed_traffic_light.Tag = new object[] { oscillator, 22, 24, 24, 28, 1, 2, false };

            oscillator = @"2o16b2o$bo16bo$bobo12bobo$2b2o12b2o2b$7bo4bo7b$5b2ob4ob2o5b$7bo4bo7b$2
                           b2o12b2o2b$bobo12bobo$bo16bo$2o16b2o";
            oscillator_snacker.Tag = new object[] { oscillator, 11, 20, 13, 22, 1, 1, false };

            oscillator = @"2o32b$bo32b$bobo25b2o3b$2b2o23bo2bo3b$7bo4bo14b3o4b$5b2ob4ob2o3bo4bo6b
                           3o$7bo4bo3b2ob4ob2o3bo3bo$2b2o14bo4bo6b3o$bobo23b3o4b$bo25bo2bo3b$2o27
                           b2o";
            oscillator_snacker_2.Tag = new object[] { oscillator, 11, 34, 13, 36, 1, 1, false };

            oscillator = "4b3o4b2$2bobobobo2b2$obo5bobo$o9bo$obo5bobo2$2bobobobo2b2$4b3o";
            oscillator_star.Tag = new object[] { oscillator, 11, 11, 15, 15, 2, 2, false };

            oscillator = @"33bo$9bo2b2o7bo10bobo$o2b2o2bobobo5bo3bo7bo2b2o$o10bo2bobo3bo4bo2bobob
                           2ob2o$o3bo4bo4b2obobo2bob2obo4b2o$3b2o4bob2obo2bobob2o4bo4bo3bo$2ob2ob
                           obo2bo4bo3bobo2bo10bo$3b2o2bo7bo3bo5bobobo2b2o2bo$2bobo10bo7b2o2bo$3bo";
            oscillator_swine.Tag = new object[] { oscillator, 10, 37, 12, 41, 1, 2, false };
            #endregion

            // T
            #region
            oscillator = @"21b2o4b2o19b$21bobo2bobo19b$23bo2bo21b$22b2o2b2o20b$21b3o2b3o19b$23bo2
                           bo21b$31bo16b$30bob2o14b$34bo13b$26bo3bo2bobo12b$26bo5bo2bo12b$26bo6b2
                           o13b$9b2o37b$8bo2bo10b3o3b3o17b$7bobobo36b$6b3obo15bo21b$6b3o17bo21b$2
                           6bo21b$12b3o33b$2o2bo16b3o24b$o2b2o5bo5bo31b$b5o4bo5bo2bo5bo17bo2b2o$1
                           0bo5bo2bo5bo17b2o2bo$19bo5bo7b3o6b5ob$b5o6b3o33b$o2b2o16b3o7bo5bo10b$2
                           o2bo26bo5bo4b5ob$31bo5bo5b2o2bo$43bo2b2o$33b3o12b$39b2o7b$38b3o7b$37bo
                           b2o7b$36bobo9b$20b3o13bo2bo8b$37b2o9b$13b2o4bo2bo25b$12bo2bo32b$12bobo
                           bo31b$13bo2bo31b$17bo30b$14bobo31b$21bo2bo23b$19b3o2b3o21b$20b2o2b2o22
                           b$21bo2bo23b$19bobo2bobo21b$19b2o4b2o";
            oscillator_traffic_circle.Tag = new object[] { oscillator, 48, 48, 50, 50, 1, 1, false };

            oscillator = @"17b2o10b$2o15bobo7b2o$2o17bo7b2o$17b3o9b4$17b3o9b$2o17bo9b$2o15bobo9b$
                           17b2o";
            oscillator_twin_bees_shuttle.Tag = new object[] { oscillator, 11, 29, 15, 34, 2, 1, false };

            oscillator = @"7b2o3b2o10b$6bo7bo9b$9bobo12b$7b2o3b2o10b4$21b3o$20b3ob$13bo10b$3o9b3o
                           9b$b3o20b$20b3ob$21b3o2$b3o20b$3o9b3o9b$13bo10b3$10b2o3b2o7b$12bobo9b$
                           9bo7bo6b$10b2o3b2o";
            oscillator_twirling_t_tetsons_2.Tag = new object[] { oscillator, 24, 24, 26, 26, 1, 1, false };

            oscillator = @"4b2o12b2o4b$2obo2bob2o4b2obo2bob2o$2o2bo4bo4bo4bo2b2o$5bo12bo5b$6bobo6
                           bobo6b$10bo13b$9bobo12b$8bo5bo9b$9bo5bo8b$12bobo9b$13bo10b$6bobo6bobo6
                           b$5bo12bo5b$2o2bo4bo4bo4bo2b2o$2obo2bob2o4b2obo2bob2o$4b2o12b2o";
            oscillator_two_pre_l_hasslers.Tag = new object[] { oscillator, 16, 24, 20, 26, 2, 1, false };
            #endregion

            // U
            #region
            oscillator = @"2b2o3b4o3b$2b2o2bo4bo2b$2obo2bo3bobob$3o4bo3bobo$13bo$13bo$b2o7bo2bo$o
                           2bo7b2ob$o13b$o13b$obo3bo4b3o$bobo3bo2bob2o$2bo4bo2b2o2b$3b4o3b2o";
            oscillator_unicycle.Tag = new object[] { oscillator, 14, 14, 18, 18, 2, 2, false };
            #endregion

            // W
            #region
            oscillator = "3bo3b$3bobob$bo5b$ob5o$bo5b$3bobob$3bo";
            oscillator_why_not.Tag = new object[] { oscillator, 7, 7, 9, 9, 1, 1, false };

            oscillator = @"11bo6b$9b2obo5b$7b2o9b$10b2o6b$7b3o8b2$3o15b$3b2o2b3ob2o5b$10b7ob$b7o1
                           0b$5b2ob3o2b2o3b$15b3o2$8b3o7b$6b2o10b$9b2o7b$5bob2o9b$6bo";
            oscillator_windmill.Tag = new object[] { oscillator, 18, 18, 20, 20, 1, 1, false };

            oscillator = @"2o12b2o$bo12bob$bobo8bobob$2b2o8b2o2b2$5b6o5b2$2b2o8b2o2b$bobo8bobob$b
                           o12bob$2o12b2o";
            oscillator_worker_bee.Tag = new object[] { oscillator, 11, 16, 13, 18, 1, 1, false };
            #endregion

            #endregion

            // Spaceships
            #region
            string spaceship;

            // Unnamed
            #region
            spaceship = @"bo$bo8bo$obo5bo3bo$8bo3b2o$5bob2o5b2o$b6o2bo6bo$2b2o6bo3b3o$10bo3bob2o
                          $13bo$18bo$17bo$17bo";
            spaceship_37_p_4_h_1_v_0.Tag = new object[] { spaceship, 12, 19, 30, 21, 17, 1, false };

            spaceship = @"46bo29b$46bo29b$48bo5bo21b$47bo6bobo19b$46bo3bo2bo22b$47bo2bobob2o20b$
                          52bob2o20b$66b2o8b$66b2o8b7$74b2o$74b2o$55b2o3bo15b$53b3o4b2o14b$48b3o
                          bo8b2o13b$52bobo2b5o14b$53bo3b3o16b3$40bo35b$40bo7bo27b$40bo5b3o27b$39
                          b2o3bobo29b$45bo2bo27b$44bo2b2o27b10$27bo48b$24b4o48b4$27bobo46b$28bo4
                          7b$2o2bo21b2o48b$3bobo20bo2bo46b$2bo16bo5b2ob2o46b$19bo56b$4b2o13bo56b
                          2$5b2o12b2o55b$4bo13bo2bo54b$2b2ob2o11bobo55b$5b2o10b2o57b$3bo13bo58b$
                          20b2o54b$20b2o54b$20b2o54b$17b2obo55b$18b3o55b$19bo56b4$7b2o67b$7b2o67
                          b7$15b2o59b$15b2o";
            spaceship_4_engine_cordership.Tag = new object[] { spaceship, 76, 76, 100, 100, 23, 23, false };

            spaceship = @"4bo5bo4b$3b3o3b3o3b$2bo2bo3bo2bo2b$b3o7b3ob$2bobo5bobo2b$4b2o3b2o4b$o4
                          bo3bo4bo$5bo3bo5b$2o3bo3bo3b2o$2bo2bo3bo2bo2b$4bo5bo";
            spaceship_44_p_5_h_2_v_0.Tag = new object[] { spaceship, 11, 15, 30, 17, 18, 1, false };

            spaceship = @"3bo11bo3b$3bo11bo3b$2bobo9bobo2b2$bo3bo7bo3bob$bob6ob6obob$o7bobo7bo$o
                          7bobo7bo$o17bo$bob2ob2o3b2ob2obo";
            spaceship_46_p_4_h_1_v_0.Tag = new object[] { spaceship, 10, 19, 30, 23, 19, 2, false };

            spaceship = @"5b3o10b3o5b$3obo7b2o7bob3o$4bo3bo2bo2bo2bo3bo4b$4bo5bo4bo5bo4b$10b2o2b
                          2o10b$7bo3bo2bo3bo7b$7bobo6bobo7b$8b10o8b$10bo4bo10b$8bo8bo8b$7bo10bo7
                          b$8bo8bo";
            spaceship_56_p_6_h_1_v_0.Tag = new object[] { spaceship, 12, 26, 30, 28, 17, 1, false };

            spaceship = @"20b2ob$20b2ob$19bo2bo$16b2obo2bo$22bo$14b2o3bo2bo$14b2o5bob$15bob5ob$1
                          6bo6b3$13b3o7b$13bo9b$11b2o10b$5b2o4bo11b$5b3o3bo11b$3bo4bo14b$3bo3bo1
                          5b$7bo15b$2b2obobo15b$2o5bo15b$2o4b2o15b$2b4o";
            spaceship_58_p_5_h_1_v_1.Tag = new object[] { spaceship, 23, 23, 50, 50, 26, 26, false };

            spaceship = @"32bo33b$30b2ob2o31b$32bo33b$47b2o17b$46bo2bo16b$33b2o11b2ob2o15b$32bo2
                          bo11bo18b$31b2o3bo10b3o16b$31bo4bo19b2o8b$31bo3bo20b2o8b$32bo2bo30b$33
                          bo32b3$21bo7bo14b2o20b$20b3o5bobo12bob2o19b$19b2ob4o2bo2bo10b2o20b2o$2
                          0b3o2bo18bo19b2o$21bo2b2o2bo37b$25b2obobo35b$29bo10b2o24b$40b2o24b2$3o
                          49b3o11b$4bo28bo22bo9b$4b2o13bo12bobo21b2o8b$4bo2bo11bo12bobo21bo2bo6b
                          $5bobo11bo13bo23bobo6b$5bo51bo8b$4b2o15bo34b2o8b$3bobo14bobo32bobo8b$5
                          b2o14bo35b2o7b$2b2o50b2o10b$4bo37bo13bo9b$42bo23b$42bo8bo14b$49bobo14b
                          $43b3o2bo3bo13b$44bob5o15b$50bo15b$45b2o19b3$7b2o57b$7b2o57b3$24b3o39b
                          $28bo37b$28b2o36b$28bo2bo34b$15b2o12bobo34b$15b2o12bo36b$28b2o36b$27bo
                          bo36b$29b2o35b$26b2o38b$28bo37b2$23b2o41b$23b2o";
            spaceship_6_engine_cordership.Tag = new object[] { spaceship, 61, 66, 100, 100, 38, 33, false };

            spaceship = @"12bob3o$4bo6bo5bo6b2obo$2b4o4b2o2bob2o3b2obob2ob3o$b2o3bob2o2b4ob5o2b2
                          o6bo$o3bob4o8bo3bo3bo3b2o$4bo14bo$5bo4bo$9bo";
            spaceship_60_p_3_h_1_v_0_3.Tag = new object[] { spaceship, 8, 33, 30, 35, 21, 1, false };

            spaceship = @"5bo7bo5b$3b2ob2o3b2ob2o3b$6b2o3b2o6b$8bobo8b$bo4bobobobo4bob$3o5bobo5b
                          3o$o5bobobobo5bo$2bo2bo2bobo2bo2bo2b$2b2o3b2ob2o3b2o2b$o7bobo7bo$o6b2o
                          b2o6bo";
            spaceship_60_v_5_v_2_v_0.Tag = new object[] { spaceship, 11, 19, 40, 23, 28, 2, false };

            spaceship = @"5b3o15b3o5b$4bo3bo13bo3bo4b$3b2o4bo11bo4b2o3b$2bobob2ob2o3b3o3b2ob2obo
                          bo2b$b2obo4bob2ob3ob2obo4bob2ob$o4bo3bo4bobo4bo3bo4bo$12bo5bo12b$2o7b2
                          o9b2o7b2o";
            spaceship_64_p_2_h_1_v_0.Tag = new object[] { spaceship, 8, 31, 30, 33, 21, 1, false };

            spaceship = @"5b3o14b$4bo3b2o12b$3b2o3bo13b$2bo5bo13b$bob2o4b2o11b$2o2bo6bo10b$3b2o2
                          bo14b$3b2ob2o14b$4bo17b$5b5o12b$6bo2b3o2b2o6b$9bob2o2bob2o3b$9bo3bobo2
                          bo3b$10b5o5bob$9bo2bo2bo5bo$21bo$16b3o3b$16bo5b$15bo6b$16b2o";
            spaceship_67_p_5_h_1_v_1.Tag = new object[] { spaceship, 20, 22, 50, 52, 29, 29, false };

            spaceship = @"27bobo41b$26bo44b$27bo2bo40b$29bobo39b$32bo11b3o24b$31b3o37b$31bo39b$2
                          9b2o40b$31bo13b3o23b$14bo14bo2bo12b3o5b2o16b$13bobo13bobo13b2o6b2o16b$
                          29bo14bo26b$13bo2bo3bob3o17b2o2bo24b$15bo4bo22bo3bo23b$16bob2obobo19bo
                          2bo24b$17b2o3bo21b3o24b$18bo52b$61b2o8b$61b2o8b2$25bo45b$25bo5b3o37b$2
                          4bobo3b2o2bo36b$25bo3bo5bo35b$25bo3bo41b$30bo2b2o25bo8b2o$bobo27b2o26b
                          obo7b2o$o58bobo9b$bo2bo54bo2bo8b$3bobo54bobo8b$6bo56bo7b$5b3o54b2o7b$5
                          bo56bo8b$3b2o66b$5bo65b$3bo2bo31bo32b$3bobo31bob2o30b$3bo36b2o29b$35bo
                          6bo28b$34bo3b4o29b$35bo4bo30b4$41b3o27b$40bo3bo26b$41b2o28b$7b2o34b2ob
                          2o23b$7b2o36b2o24b3$34bo36b$33bobo35b$33bobo35b$33bo2bo34b$15b2o17bobo
                          34b$15b2o20bo33b$36b2o33b$36bo34b5$23b2o46b$23b2o";
            spaceship_7_engine_cordership.Tag = new object[] { spaceship, 65, 71, 94, 100, 28, 28, false };

            spaceship = @"2bo12bo2b$bobo10bobob$2ob2o8b2ob2o$2o14b2o$2bo12bo2b$2b4o6b4o2b$2bo2b2
                          o4b2o2bo2b$3b2o2bo2bo2b2o3b$4b2ob4ob2o4b$5bobo2bobo5b$6bo4bo6b2$5bo6bo
                          5b$3b2ob2o2b2ob2o3b$4bo8bo4b$4b2o6b2o";
            spaceship_70_p_5_h_2_v_0.Tag = new object[] { spaceship, 16, 18, 40, 20, 22, 1, false };

            spaceship = @"34bob$33bobo$31b2o2bo2$30bo5b$27b2o2b2o3b$27b2o7b$27bo8b$25bobo8b$25b2
                          o9b$23bo2bo9b$22bo4bo8b$21bo5bo8b$18bo3bo13b$17bobo3b2o11b$16b2ob2o15b
                          $15b2o4bobo12b$14b2o7bo12b$13bo9bo12b$14b2o20b$15bo20b$12bo3bo19b$11bo
                          bo22b$10bo3bob3o17b$14bo21b$8b2o26b$9b2o25b$5b4o2b2o23b$5b2o29b2$4bo31
                          b$2bo2bo30b$2bo2bo30b$bo34b$o35b$b2o";
            spaceship_77_p_6_h_1_v_1.Tag = new object[] { spaceship, 36, 36, 70, 70, 33, 33, false };

            spaceship = @"9b3o11b$8bo14b$7bo15b$11b2o10b$8b2obo11b$14b3o6b$11bo2b2o2b2o3b$2bo8b2
                          obo3b2o3b$bo2bo6bo2b2o7b$o3bo18b$o11bo2bo7b$o2b2ob3o3bo3b2ob2o2b$3bo3b
                          o2b2o2bo2bo5b$17b2o2bob$5b4o3bo5bo3bo$5b2obobo10bob$5bo5bo6b2o3b$11b3o
                          9b$6b2o5b2obo6b$6b2o3bo4bo6b$11bo11b$13bobo7b$14bo";
            spaceship_86_p_5_h_1_v_1.Tag = new object[] { spaceship, 23, 23, 60, 60, 36, 36, false };
            #endregion

            // B
            #region
            spaceship = @"3b2o3b$2b2o4b$4bo3b$6b2o$5bo2b2$4bo2bo$b2obo3b$2o4bob$2bobo2bo$7bo$4bo
                          2bo$5bobo$5bobo$6b2o$6bo";
            spaceship_b_29.Tag = new object[] { spaceship, 16, 8, 50, 50, 31, 31, false };

            spaceship = @"7bo45bo7b$5b4o5b3o27b3o5b4o5b$b2ob2o3bo3bo33bo3bo3b2ob2ob$b2o2bo5b3o4b
                          o3bo15bo3bo4b3o5bo2b2ob$o2bo7bobob5ob4o11b4ob5obobo7bo2bo$9bo6bo2bobo3
                          bob2o3b2obo3bobo2bo6bo9b$10bo16b2o3b2o16bo10b$22bo3bo2bobo2bo3bo22b$22
                          bobo11bobo22b$23bo13bo23b$22b2o3bo5bo3b2o22b$21b2o2b4o3b4o2b2o21b$25bo
                          3bobo3bo25b$21b2o6bobo6b2o21b$22bo4bobobobo4bo22b$21bo4b2obobob2o4bo21
                          b$22b2obo3bobo3bob2o22b$22b4o2b2ob2o2b4o22b$21b2o3bo2bobo2bo3b2o21b$28
                          bo3bo";
            spaceship_barge.Tag = new object[] { spaceship, 20, 61, 50, 63, 29, 1, false };

            spaceship = @"14b3ob3o14b$13bo2bobo2bo13b$12bo3bobo3bo12b$7b3obo2bobobobo2bob3o7b$6b
                          o2bobo4bobo4bobo2bo6b$5bo3bobobobo3bobobobo3bo5b$5bo23bo5b$7bo19bo7b$4
                          bobo21bobo4b$3b2obob3o13b3obob2o3b$2bobobo3bo13bo3bobobo2b$b2obo25bob2
                          ob$o3bo5b2o11b2o5bo3bo2$2ob2o25b2ob2o";
            spaceship_barge_2.Tag = new object[] { spaceship, 15, 35, 50, 37, 34, 1, false };

            spaceship = @"3b3o12b$3bo2b3o9b$4bobo11b$2o7bo8b$obo4bo2bo7b$o8b2o7b$b2o15b$bo2bo5bo
                          b2o4b$bo9b2obo3b$3bobo6b2o2bob$4b2obo4b2o3bo$8bo7bob$7b4o3bobob$7bob2o
                          3b4o$8bo3b2obo2b$13b2o3b$9bob3o4b$10bo2bo";
            spaceship_big_glider.Tag = new object[] { spaceship, 18, 18, 50, 50, 30, 30, false };

            spaceship = @"b3o9b3ob$obob2o5b2obobo$obobo7bobobo$bob2ob2ob2ob2obob$5bobobobo5b$3bo
                          bobobobobo3b$2b2obobobobob2o2b$2b3o2bobo2b3o2b$2b2o2bo3bo2b2o2b$bo4b2o
                          b2o4bob$bo13bo";
            spaceship_brain.Tag = new object[] { spaceship, 11, 17, 50, 19, 38, 1, false };
            #endregion

            // C
            #region
            spaceship = @"3o10b$o9b2ob$bo6b3obo$3b2o2b2o4b$4bo8b$8bo4b$4b2o3bo3b$3bobob2o4b$3bob
                          o2bob2ob$2bo4b2o4b$2b2o9b$2b2o";
            spaceship_canada_goose.Tag = new object[] { spaceship, 12, 13, 40, 40, 27, 26, false };

            spaceship = "4b6o$2b2o5bo$2obo5bo$4bo3bob$6bo3b$6b2o2b$5b4ob$5b2ob2o$7b2o";
            spaceship_coe_ship.Tag = new object[] { spaceship, 9, 10, 14, 40, 3, 2, false };

            spaceship = @"8b2o3b$7b2o4b$9bo3b$11b2o$10bo2b2$9bo2bo$b2o5b2o3b$2o5bo5b$2bo4bobo3b$
                          4b2o2bo4b$4b2o";
            spaceship_crab.Tag = new object[] { spaceship, 12, 13, 40, 40, 27, 26, false };
            #endregion

            // D
            #region
            spaceship = @"7bo7b$6bobo6b$5bo3bo5b$6b3o6b2$4b2o3b2o4b$2bo3bobo3bo2b$b2o3bobo3b2ob$
                          o5bobo5bo$bob2obobob2obo";
            spaceship_dart.Tag = new object[] { spaceship, 10, 15, 40, 17, 29, 1, false };

            spaceship = @"12bo16b$12b2o14bo$10bob2o5bobo4b2o$5bo3bo3b3o2bo4bo5b$2o3bo2bo6bobo5b3
                          o2bo$2o3bob2o6bo3bobobo5b$2o3bo10bobo7b2o$5b2o14bo6bo$7bo12bobo6b$7bo1
                          2bobo6b$5b2o14bo6bo$2o3bo10bobo7b2o$2o3bob2o6bo3bobobo5b$2o3bo2bo6bobo
                          5b3o2bo$5bo3bo3b3o2bo4bo5b$10bob2o5bobo4b2o$12b2o14bo$12bo";
            spaceship_dragon.Tag = new object[] { spaceship, 18, 29, 20, 50, 1, 20, false };
            #endregion

            // E
            #region
            spaceship = @"bo2bo5b2o$o8b4o$o3bo3b2ob2o$4o5b2o4$11b2o$2b3o7b2o$2bo6bo2bo$2bobo6bo$
                          3b2o3b2o3$bo2bo$o$o3bo$4o";
            spaceship_ecologist.Tag = new object[] { spaceship, 18, 14, 21, 60, 1, 41, false };

            spaceship = "8bo7b$7b4o5b$2bo3bo3b2ob2ob$b4o5bo2b2ob$o3bo7bo2bo$bobo2bo9b$5bo";
            spaceship_edge_repair_spaceship_1.Tag = new object[] { spaceship, 7, 16, 40, 18, 32, 1, false };

            spaceship = @"obo19b$o21b$obo2bo2bo13b$3bo18b$4bob2obo12b$5b3obo12b$4bo4bo2bob2o6b$b
                          o3b2o3bob4o6b$b2o2bo2bob2o4bo4bo$bo8b2o4bo2b3o$6b2o3b2o3bo2b2o";
            spaceship_edge_repair_spaceship_2.Tag = new object[] { spaceship, 11, 22, 15, 50, 2, 26, false };

            spaceship = @"8bo12b$6b2obo11b$4b2o3bo11b$3bo3b2o12b$2bo4b2obo10b$bo2b2ob3obo9b$bo19
                          b$3bobo3b2o10b$2o3bobo13b$o3bo3b2o11b$bo2b2o2bo2bo9b$4b2o3bo8bo2b$10bo
                          bo3bobobo$12b2ob3o2bo$12b2o4b2ob$13bob2o4b2$11bobo7b$11bo9b$11b3o7b$12
                          bobo";
            spaceship_enterprise.Tag = new object[] { spaceship, 21, 21, 70, 70, 48, 48, false };
            #endregion

            // F
            #region
            spaceship = @"2bo31b$bobo30b$bobo22bobo3bob$bo23b2obobo2bo$11b3o8bo9bob$2o9b2o2bob2o
                          3bo2b4o5b$bobo9b4o2bobo2b2o4b2o2b$b2o8bo2bo3b3o5b3o5b$2bo7bo4bo2b2o2b2
                          o2bo2bo4b$3bo2bo3bo4bo2b3obobo4b2o3b$7bob2o4bo2b4o5bo6b$4b2o3b2o4bo2b4
                          o5bo6b$4bobo3bo4bo2b3obobo4b2o3b$3b2o5bo4bo2b2o2b2o2bo2bo4b$4bobo4bo2b
                          o3b3o5b3o5b$5bo7b4o2bobo2b2o4b2o2b$11b2o2bob2o3bo2b4o5b$11b3o8bo9bob$2
                          5b2obobo2bo$26bobo3bo";
            spaceship_fly.Tag = new object[] { spaceship, 20, 34, 22, 60, 1, 25, false };
            #endregion

            // H
            #region
            spaceship = @"5o13b$o4bo7b2o3b$o11b2ob3o$bo9b2ob4o$3b2o3b2ob2o2b2ob$5bo4bo2bo4b$6bob
                          obobo5b$7bo10b$7bo10b$6bobobobo5b$5bo4bo2bo4b$3b2o3b2ob2o2b2ob$bo9b2ob
                          4o$o11b2ob3o$o4bo7b2o3b$5o";
            spaceship_hammerhead.Tag = new object[] { spaceship, 16, 18, 22, 50, 3, 31, false };

            spaceship = "3b2o2b$bo4bo$o6b$o5bo$6o";
            spaceship_heavyweight_spaceship.Tag = new object[] { spaceship, 5, 7, 9, 30, 1, 22, false };

            spaceship = @"4o5bo2bo$o3bo3bo4b$o7bo3bo$bo2bo3b4ob2$5b2o6b$5b2o6b$5b2o6b2$bo2bo3b4o
                          b$o7bo3bo$o3bo3bo4b$4o5bo2bo";
            spaceship_hivenudger.Tag = new object[] { spaceship, 13, 13, 17, 40, 2, 26, false };
            #endregion

            // L
            #region
            spaceship = "bo2bo$o4b$o3bo$4o";
            spaceship_lightweight_spaceship.Tag = new object[] { spaceship, 4, 5, 7, 16, 1, 10, false };

            spaceship = "b2o2bob2o$o2bo2b2o$bobo$2bo$8bo$6b3o$5bo$6bo$7b2o";
            spaceship_loafer.Tag = new object[] { spaceship, 9, 9, 12, 25, 2, 15, false };

            spaceship = @"11b3o$11bo$2o2bobo5bo2b2o$obob2o9b2o$o4bo2bo2b2o$6bo5b2o$2b2o7bo2bo$2b
                          2o$13bo16b3o$20b3o7bo$20bo10bo2b2o$9b2o2bobo5bo2b2o8b2o$9bobob2o9b2o4b
                          2o$9bo4bo2bo2b2o9b2o$15bo5b2o7bo2bo$11b2o7bo2bo$11b2o19bo2bo$22bo9bo3b
                          o$33b3obo$38bo$18b2o2bobo13bo$18bobob2o13bo$18bo4bo2b2o13b2o$24bo3bo6b
                          2o2b2o2bo$20b2o6bo6bo2bo$20b2o4bobo4b2o$27bo5bo3bo3bo$28bo2bo4b2o$29b2
                          o3bo5bobo$33bo8b2o$33bo4bo$32bo3bo$32bo5b2o$33bo5bo";
            spaceship_lobster.Tag = new object[] { spaceship, 34, 44, 70, 70, 35, 25, false };
            #endregion

            // M
            #region
            spaceship = "3bo2b$bo3bo$o5b$o4bo$5o";
            spaceship_middleweight_spaceship.Tag = new object[] { spaceship, 5, 6, 9, 30, 1, 23, false };
            #endregion

            // N
            #region
            spaceship = @"10b2obo7b$6b3obob3o6b$2bobo10bo3b2o$2o4b2o5bo3b4o$2bob2o2bo4b3obo3b$8b
                          o4bo7b$2bob2o2bo4b3obo3b$2o4b2o5bo3b4o$2bobo10bo3b2o$6b3obob3o6b$10b2o
                          bo";
            spaceship_non_monotonic_spaceship_1.Tag = new object[] { spaceship, 11, 21, 15, 50, 2, 28, false };
            #endregion

            // O
            #region
            spaceship = @"3b2o9b$3bobo8b$3bo10b$2obo10b$o4bo8b$ob2o6b3ob$5b3o4b2o$6b3obobob$13bo
                          $6bobo5b$5b2obo5b$6bo7b$4b2obo6b$7bo6b$5b2o";
            spaceship_orion.Tag = new object[] { spaceship, 15, 14, 50, 50, 34, 35, false };

            spaceship = @"b2o10b$2o11b$2bo10b$4bo4b3ob$4b3o4b2o$5b3obobob$12bo$5bobo5b$4b2obo5b$
                          5bo7b$3b2obo6b$6bo6b$4b2o";
            spaceship_orion_2.Tag = new object[] { spaceship, 13, 13, 50, 50, 36, 36, false };
            #endregion

            // P
            #region
            spaceship = @"3b3o10b3o23b3o10b3o3b$4bobo8bobo25bobo8bobo4b$6bo8bo29bo8bo6b$bob5o6b5
                          obo19bob5o6b5obob$bob2o3b2o2b2o3b2obo19bob2o3b2o2b2o3b2obob$2o5b3o2b3o
                          5b2o17b2o5b3o2b3o5b2o$5b2ob2o2b2ob2o27b2ob2o2b2ob2o5b2$4bo12bo25bo12bo
                          4b$5b3o6b3o27b3o6b3o5b$4b4o6b4o25b4o6b4o4b$3b2o12b2o23b2o12b2o3b$4bo12
                          bo25bo12bo4b$2b3o12b3o21b3o12b3o2b$2bo16bo21bo16bo2b$5bo10bo27bo10bo5b
                          $3bobo10bobo23bobo10bobo3b$2bo3b2o6b2o3bo21bo3b2o6b2o3bo2b$3bo5bo2bo5b
                          o23bo5bo2bo5bo3b$9bo2bo35bo2bo9b$bob2o4bo2bo4b2obo19bob2o4bo2bo4b2obob
                          $bo3b3obo2bob3o3bo19bo3b3obo2bob3o3bob$2bo6bo2bo6bo21bo6bo2bo6bo2b$bo2
                          b2o3bo2bo3b2o2bo19bo2b2o3bo2bo3b2o2bob$bo2b2obobo2bobob2o2bo19bo2b2obo
                          bo2bobob2o2bob$5bo2bo4bo2bo27bo2bo4bo2bo5b$6bobo4bobo29bobo4bobo6b$27b
                          o5bo27b$4b2obob4obob2o9bo5bo9b2obob4obob2o4b$10b2o14bobo3bobo14b2o10b$
                          2bob2o10b2obo7bo5bo7bob2o10b2obo2b$2o2bo3b2o2b2o3bo2b2o5bo5bo5b2o2bo3b
                          2o2b2o3bo2b2o$b2obo12bob2o19b2obo12bob2ob$3b2obo8bob2o23b2obo8bob2o3b$
                          4bo2bo6bo2bo25bo2bo6bo2bo4b$b2o2bo2bo4bo2bo2b2o4b2o7b2o4b2o2bo2bo4bo2b
                          o2b2ob$bo5b2o4b2o5bo5bobo3bobo5bo5b2o4b2o5bob$b3o3bo6bo3b3o6bo5bo6b3o3
                          bo6bo3b3ob$3b4o8b4o23b4o8b4o3b$4bo2bo6bo2bo25bo2bo6bo2bo4b$4bo12bo25bo
                          12bo4b$4bob2o6b2obo25bob2o6b2obo4b$5bo10bo27bo10bo";
            spaceship_p_15_pre_pulsar_spaceship.Tag = new object[] { spaceship, 43, 61, 100, 63, 56, 1, false };

            spaceship = @"31bo5bo31b$30b3o3b3o30b4$2b2o5b2o5b2o5b2o19b2o5b2o5b2o5b2o2b$o3bo3bo2b
                          o3bo2bo3bo3bo15bo3bo3bo2bo3bo2bo3bo3bo$o3bobob3o5b3obobo3bo15bo3bobob3
                          o5b3obobo3bo$o5b2o2b3ob3o2b2o5bo15bo5b2o2b3ob3o2b2o5bo$2ob3o15b3ob2o15
                          b2ob3o15b3ob2o$o25bo15bo25bo$bo3bo15bo3bo17bo3bo15bo3bob$2bo2bo15bo2bo
                          19bo2bo15bo2bo";
            spaceship_pre_pulsar_spaceship.Tag = new object[] { spaceship, 13, 69, 50, 75, 36, 3, false };

            spaceship = @"3bo7b$bo2bo6b$o3bo6b$o3bo6b$2obo7b$5b2o4b$2bo2b2o4b$4bo6b$7b3ob$6b5o$5
                          b2ob3o$6b2o";
            spaceship_pushalong_1.Tag = new object[] { spaceship, 12, 11, 16, 40, 1, 28, false };
            #endregion

            // S
            #region
            spaceship = @"bo2bo15b$o19b$o3bo15b$4o9b2o5b$6b3o5b2o4b$6b2ob2o6b3o$6b3o5b2o4b$4o9b2
                          o5b$o3bo15b$o19b$bo2bo";
            spaceship_schick_engine.Tag = new object[] { spaceship, 11, 20, 13, 40, 1, 19, false };

            spaceship = @"2b2o30b$b3ob3o26b$3bo3b2o25b$2ob2o2bo2bobo21b$o6bob4o21b$6b2obo2bo21b$
                          o4b2o3bob2o20b$2o8bobo2bo18b$o2b5ob3o4bo17b$bo9bo4bo17b$b2obo2bo3bo3bo
                          18b$2bo9b2o20b$9b2o2b2o4bo14b$2bo3b2obo4b3o2bo14b$3b2obob5o5bob2o12b$7
                          b2ob2ob2ob2o2b3o11b$17bobo4bo9b$8b2o4bobo3b6o8b$13bo7b2o11b$13bo3bo16b
                          $13bo4bo3b3o6bo2b$14b5o3b2ob2o2b2ob2o$17b2obo2bob2o2bo4b$16b2o5bo4bo2b
                          3o$15b3obobobobo8b$22bo2bo8b$18bobo13b$19bo14b2$21b3o10b$21b3o10b2$20b
                          2o12b$20b2o12b$21bo";
            spaceship_seal.Tag = new object[] { spaceship, 35, 34, 70, 70, 34, 35, false };

            spaceship = "bo6b$o5bob$o5bob$5obob2$4b2o2b$2bo4bo$bo6b$bo5bo$b6o";
            spaceship_sidecar.Tag = new object[] { spaceship, 10, 8, 14, 30, 1, 21, false };

            spaceship = @"bo36b$bo36b$o37b$b3o17b3o3b3o8b$b2obo9bo3bobo6b3o8b$2bo11b2obo7bo4b4o4
                          b$6bo6bo3bobo3b2obo5b2o4b$3bo2bob3o3b2o9bo8b2obo$3b2obo5bo5bo17bob$9bo
                          b7o20b2$9bob7o20b$3b2obo5bo5bo17bob$3bo2bob3o3b2o9bo8b2obo$6bo6bo3bobo
                          3b2obo5b2o4b$2bo11b2obo7bo4b4o4b$b2obo9bo3bobo6b3o8b$b3o17b3o3b3o8b$o3
                          7b$bo36b$bo";
            spaceship_snail.Tag = new object[] { spaceship, 21, 38, 23, 60, 1, 21, false };

            spaceship = @"10b2obo12b2o2b$6b3obob3o8bob3o2b$2bobo10bo3b2obo4bo2b$2o4b2o5bo3b4obo3
                          b2o2b$2bob2o2bo4b3obo5b4obob$8bo4bo13b3o$2bob2o2bo4b3obo5b4obob$2o4b2o
                          5bo3b4obo3b2o2b$2bobo10bo3b2obo4bo2b$6b3obob3o8bob3o2b$10b2obo12b2o";
            spaceship_sparky.Tag = new object[] { spaceship, 11, 30, 15, 60, 2, 28, false };

            spaceship = @"9bo7bo9b$3b2obobob2o3b2obobob2o3b$3obob3o9b3obob3o$o3bobo5bobo5bobo3bo
                          $4b2o6bobo6b2o4b$b2o9bobo9b2ob$b2ob2o15b2ob2ob$5bo15bo";
            spaceship_spider.Tag = new object[] { spaceship, 8, 27, 40, 33, 31, 3, false };

            spaceship = @"bo10b2o10b$5o6b2o11b$o2b2o8bo7b2ob$2b2obo5b2o6b3obo$11b2o3bob2o4b$5bob
                          o6b2o8b$10b3obo4bo4b$7b3o3bo4bo5b$8bo7bo7b$8bo6bo8b2$11bo";
            spaceship_swan.Tag = new object[] { spaceship, 12, 24, 50, 50, 37, 25, false };
            #endregion

            // T
            #region
            spaceship = @"b3o7bo$b2o2bob2ob2o$3b3o4bob$bo2bobo3bob$o4bo4bob$o4bo4bob$bo2bobo3bob
                          $3b3o4bob$b2o2bob2ob2o$b3o7bo";
            spaceship_turtle.Tag = new object[] { spaceship, 10, 12, 14, 40, 2, 26, false };
            #endregion

            // W
            #region
            spaceship = @"10bo3bo7b$7bobob2ob3o5b$6bo2bo6b2ob2ob$b2o2b2o2bo3bo2bo2b2ob$b2obob2o2
                          bo2bo4bo2bo$o3bo4b2o11b$obobo2bo2b2o10b$9bo12b$b3o";
            spaceship_wasp.Tag = new object[] { spaceship, 9, 22, 40, 24, 29, 1, false };

            spaceship = @"bo12bo$bo12bo$obo10bobo$bo12bo$bo12bo$2bo3b4o3bo2b$6b4o6b$2b4o4b4o2b2$
                          4bo6bo4b$5b2o2b2o";
            spaceship_weekender.Tag = new object[] { spaceship, 11, 16, 25, 20, 13, 2, false };

            spaceship = @"4b2o17b$4bobo16b$4bo18b$7bo15b$3o4bobo13b$o6bo2bo3b3o6b$bo7b2o2bo2b2o5
                          b$3b3o7bo5b2o2b$13b2o4b3ob$4bobo3b3o9bo$5b2o2bo2bo6bo2bo$9bo9bo3b$9b2o
                          8bobob$6b3o14b$5bo2bo14b$5bo17b$5b2o16b$6bo16b2$7b2ob3o10b$7b2o14b$8bo
                          3bo10b$9b2o";
            spaceship_wing.Tag = new object[] { spaceship, 23, 23, 60, 60, 36, 36, false };
            #endregion

            // X
            #region
            spaceship = "2bo6b$2o7b$o2b3o2bo$o4b3ob$b3o2b2ob2$b3o2b2ob$o4b3ob$o2b3o2bo$2o7b$2bo";
            spaceship_x_66.Tag = new object[] { spaceship, 11, 9, 13, 30, 1, 20, false };
            #endregion

            #endregion
        }
        #endregion
    }
}