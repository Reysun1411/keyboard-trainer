using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Threading;
using System.Configuration;
using System.Collections.ObjectModel;

namespace Keyboard_trainer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        Stopwatch stopWatch = new Stopwatch();
        string theme = "Light";

        // Изначальный текст
        string txt = "Раз, два, три, ёлочка - гори!";
        string txtToType, txtTyped, neededLetter, results, currentTime;
        int mistakes = 0, txtLength = 29;
        double speed;
        bool handle = false;
        SolidColorBrush clrRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Red"));
        SolidColorBrush clrGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3A3A3A"));
        SolidColorBrush clrWhite = new SolidColorBrush((Color)ColorConverter.ConvertFromString("White"));
        SolidColorBrush clrGold = new SolidColorBrush((Color)ColorConverter.ConvertFromString("DarkOrange"));
        SolidColorBrush clrBlack = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black"));
        SolidColorBrush clrGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5CC73F"));
        SolidColorBrush clrGhWhite = new SolidColorBrush((Color)ColorConverter.ConvertFromString("GhostWhite"));


        public MainWindow()
        {
            InitializeComponent();
            cmb_Theme.Items.Add("Light");
            cmb_Theme.Items.Add("Dark");
            var window = Window.GetWindow(this);
            dispatcherTimer.Tick += new EventHandler(dt_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);

            // Присвоение DataGrid'у источника элементов от файла с рекордами
            Scoreboard.ItemsSource = ScoreService.ReadFile(@"scores/scores.txt");
        }

        // Класс, определяющий столбцы в Scoreboard
        public class Score
        {
            [System.ComponentModel.DisplayName("Название файла")]
            public string FileName { get; set; }
            [System.ComponentModel.DisplayName("Пользователь")]
            public string PlayerName { get; set; }
            [System.ComponentModel.DisplayName("Время")]
            public string Time { get; set; }
            [System.ComponentModel.DisplayName("Ошибки")]
            public int Mistakes { get; set; }
            [System.ComponentModel.DisplayName("Скорость")]
            public string Speed { get; set; }
        }

        // Класс, читающий файл рекордов и делящий информацию на элементы при помощи Split
        public static class ScoreService
        {
            public static List<Score> ReadFile(string filepath)
            {
                var lines = File.ReadAllLines(filepath);

                var data = from l in lines.Skip(1)
                           let split = l.Split(';')
                           select new Score
                           {
                               FileName = split[0],
                               PlayerName = split[1],
                               Time = split[2],
                               Mistakes = int.Parse(split[3]),
                               Speed = split[4]
                           };
                return data.ToList();
            }
        }


        // Задаёт русские названия столбцам при помощи PropertyDescriptor
        private void Scoreboard_ColumnsName(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyDescriptor is System.ComponentModel.PropertyDescriptor descriptor)
            {
                e.Column.Header = descriptor.DisplayName ?? descriptor.Name;
            }
        }


        // Смена темы программы светлая/темная
        private void cmb_Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var window = Window.GetWindow(this);
            theme = cmb_Theme.SelectedItem.ToString();
            // В основном изменяются локальные кисти, которые применены к стилям,
            // которые в свою очередь применены к разным элементам в XAML
            if (theme == "Dark")
            {
                window.Background = clrGray;
                this.Resources["themeBrush"] = clrGold;
                this.Resources["buttonBrush"] = clrGray;
                this.Resources["importantLblBrush"] = clrGhWhite;
                this.Resources["accentBrush"] = clrGold;
                txtInput.Background = clrGray;
                txtInput.BorderBrush = clrGold;
                txt_Name.Background = clrGray;
                txt_Name.BorderBrush = clrGold;
                EnteredText.Foreground = clrGold;
            }
            else
            {
                window.Background = clrWhite;
                this.Resources["themeBrush"] = clrBlack;
                this.Resources["buttonBrush"] = clrGhWhite;
                this.Resources["importantLblBrush"] = clrBlack;
                this.Resources["accentBrush"] = clrGreen;
                txtInput.ClearValue(Control.BackgroundProperty);
                txtInput.ClearValue(Control.BorderBrushProperty);
                txt_Name.ClearValue(Control.BackgroundProperty);
                txt_Name.ClearValue(Control.BorderBrushProperty);
                EnteredText.Foreground = clrGreen;
            }
        }


        // Кнопка загрузки .txt файла, открывающий диалог с проводником
        private void btn_ImportTxt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opendialog = new OpenFileDialog();
            opendialog.Title = "Загрузить текстовый файл...";
            opendialog.CheckPathExists = true;
            opendialog.Filter = "Text Files|*.txt";

            if (opendialog.ShowDialog() == true)
            {
                // Если файл выбран, то запускается функция загрузки текстового файла
                loadTxtFile(opendialog.FileName);
            }
        }


        // Выбор заранее заготовленных текстов для печати
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var rbtn = sender as RadioButton;
            string btnname = rbtn.Name;
            string sample = String.Empty;
            var samples = new Dictionary<string, string>()
            {
                {"rbtn_ru1", "Французские булки.txt"},
                {"rbtn_ru2", "Тютчев про Россию.txt"},
                {"rbtn_ru3", "Высоцкий - Песня о друге.txt"},
                {"rbtn_ru4", "КИШ - Лесник.txt"},
                {"rbtn_ru5", "Крылов - Лебедь рак и щука.txt"},
                {"rbtn_en1", "Brown fox lazy dog.txt"},
                {"rbtn_en2", "John Lennon about happiness.txt"},
                {"rbtn_en3", "The Beatles - Yellow submarine.txt"}
            };

            if (samples.ContainsKey(btnname))
            {
                sample = samples[btnname];
            }

            lbl_FileName.Content = rbtn.Content;
            sample = @"samples\" + sample;
            // Файл открывается отдельной функцией
            loadTxtFile(sample);
        }


        // Загрузка текстового файла в программу
        void loadTxtFile(string openedFile)
        {
            string readTxt;
            var extension = System.IO.Path.GetExtension(openedFile);
            var name = System.IO.Path.GetFileName(openedFile);
            var path = System.IO.Path.GetDirectoryName(openedFile);

            // Проверка на то, был ли выбран текстовый файл
            if (extension == ".txt")
            {
                // Проверка на ; в названии файла (этот символ портит таблицу рекордов)
                if (name.Contains(";") == false)
                {
                    try
                    {
                        // Чтение файла
                        using (var sr = new StreamReader(openedFile))
                        {
                        
                            readTxt = sr.ReadToEnd(); 
                        }
                        readTxt = readTxt.Replace("\r\n", " "); // Замена отступов на пробелы
                        if (String.IsNullOrEmpty(readTxt))
                        {
                            // Проверка на пустой документ
                            MessageBox.Show("Вы загрузили пустой документ", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                        {
                            txt = readTxt;
                            txtLength = txt.Length;
                            lbl_FileName.Content = name;
                            MessageBox.Show("Текст загружен. Нажмите \"Обновить\"", "Успех",
                                MessageBoxButton.OK);
                        }
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Ошибка чтения файла", "Ошибка", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("В названии файла содержится запрещённый символ \";\"",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Неподдерживаемое разрешение файла", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Кнопка "Сохранить результат", заносит результат в список
        private void btn_SaveResults_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;

            results = Convert.ToString(lbl_FileName.Content)
                + "; " + txt_Name.Text + "; " + currentTime + "; " + mistakes + "; " + lbl_Speed.Content;
            File.AppendAllText(@"scores\scores.txt", Environment.NewLine + results);

            Scoreboard.ItemsSource = ScoreService.ReadFile(@"scores/scores.txt");
        }


        // Ход тика
        void dt_Tick(object sender, EventArgs e)
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan ts = stopWatch.Elapsed;
                currentTime = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                clocktxt.Content = currentTime;
            }
        }


        // Проверка на то, чтобы поле для имени не содержало ; и не превышало 20 символов
        private void txt_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txt_Name = sender as TextBox;
            if (String.IsNullOrEmpty(txt_Name.Text))
            {
                txt_Name.Text = "User";
            }
            else if (txt_Name.Text.Length > 20)
            {
                txt_Name.Text = (txt_Name.Text).Remove(txt_Name.Text.Length - 1, 1);
            }
            else if (txt_Name.Text.Contains(";"))
            {
                txt_Name.Text = txt_Name.Text.Replace(";", String.Empty);
            }
        }


        // "Обновить"
        private void btn_Load_Click(object sender, RoutedEventArgs e)
        {
            // Ресет секундомера и всех счетчиков
            if (stopWatch.IsRunning)
            {
                stopWatch.Stop();
            }

            btn_SaveResults.IsEnabled = false;
            stopWatch.Reset();
            clocktxt.Content = "00:00:00";
            mistakes = 0;
            lbl_Mistakes.Content = "0";
            lbl_Speed.Content = "0 сим/с";

            // Перемещение текста в txtToType и обнуление txtTyped
            txtToType = txt;
            txtTyped = "";

            // Перемещение текста в текстовые блоки
            toEnterText.Text = txtToType;
            EnteredText.Text = "";
            typedChar.Text = "";
            needL.Text = "Начните вводить текст в поле";

            // Обнуление прогресс бара и присвоение ему максимального значения
            // Максимальное значение соответствует длинне текста
            progressBar.Value = 0;
            progressBar.Maximum = txtLength;

            // Определение буквы, которую надо ввести
            neededLetter = Convert.ToString(txtToType[0]);

            // Готовность к печатанию
            handle = true;
        }


        // Ввод текста в поле txtInput
        private void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txtInput = sender as TextBox;

            // Если изменение поля txtInput не является его очисткой из-за txtInput.Clear(),
            if (txtInput.Text != "")
            {
                // то напечатанный символ фиксируется в typedChar
                typedChar.Text = txtInput.Text;
            }

            if (handle)
            {
                // Если секундомер не стартовал и ещё есть текст для печати,
                // то секундомер стартует
                if (stopWatch.IsRunning == false && 
                    String.IsNullOrEmpty(txtToType) == false)
                {
                    stopWatch.Start();
                    dispatcherTimer.Start();
                }

                // Пользователь написал нужный символ
                if (txtInput.Text == neededLetter)
                {

                    needL.Foreground = (SolidColorBrush)this.Resources["importantLblBrush"];
                    // Последний символ txtToType переносится в txtTyped
                    txtTyped = txtTyped + txtToType[0];
                    txtToType = txtToType.Remove(0, 1);
                    // Progress Bar обновляется
                    progressBar.Value += 1;

                    
                    if (txtTyped.Length > 35)
                    {
                        // Если в txtTyped больше 35 символов, то последний символ удаляется
                        // Это чтобы избежать графических артефактов
                        txtTyped = txtTyped.Remove(0, 1);
                    }
                    EnteredText.Text = txtTyped;
                    toEnterText.Text = txtToType;


                    if (txtToType.Length > 0)
                    {
                        // Устанавливается следующий нужный символ
                        neededLetter = Convert.ToString(txtToType[0]);
                        needL.Text = neededLetter;
                    }
                    // Если в txtToType не осталось символов, то обработка заканчивается
                    else
                    {
                        endOfTyping();
                    }
                    
                }

                // Подсчёт ошибок
                // Т.к. очистка текстбокса тоже считается за изменение тексбокса,
                // ставится условие, что значение не равно пустоте ""
                else if (txtInput.Text != "")
                {
                    mistakes++;
                    lbl_Mistakes.Content = Convert.ToString(mistakes);
                    needL.Foreground = clrRed;
                }
            }
            txtInput.Clear();
        }


        // Пользователь дописал до конца, txtToType больше нет
        void endOfTyping()
        {
            // Секундомер останавливается
            stopWatch.Stop();
            needL.Text = "Ввод текста окончен";
            neededLetter = String.Empty;

            // Вычисление затраченного времени
            TimeSpan ts = stopWatch.Elapsed;
            int totalTime = (ts.Minutes * 60) + ts.Seconds;
            // Деление длинны текста на затраченное время для получения скорости печати
            speed = (double)txtLength / (double)totalTime;
            speed = Math.Round(speed, 1);
            lbl_Speed.Content = String.Format("{0} сим/с", speed);

            // Кнопка сохранения результатов теперь доступна, а обработка handle останавливается
            btn_SaveResults.IsEnabled = true;
            handle = false;

            // Определение животного по итоговой скорости
            string animal = string.Empty;
            if (speed < 3.0)
            {
                animal = "🐢 Черепаха";
            }
            else if (speed < 4.5)
            {
                animal = "🐸 Лягушка";
            }
            else if (speed < 6.0)
            {
                animal = "🐰 Кролик";
            }
            else if (speed < 8.0)
            {
                animal = "🐱 Кот";
            }
            else if (speed < 10.0)
            {
                animal = "🐴 Конь";
            }
            else if (speed >= 10.0)
            {
                animal = "🐲 Дракон";
            }

            MessageBox.Show("Ваша средняя скорость печати: " + Convert.ToString(speed) +
                " символов/с\r\nВаше животное - " + animal,
                "Результаты", MessageBoxButton.OK);
        }
    }
}
