﻿using GUI_DB.Models;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GUI_DB
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private BindingList<TaskModel> tasksList;
        private BindingList<WorkerModel> workersList;
        private BindingList<ClientModel> clientsList;
        private WorkerModel currentUser;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            LoadGif.Visibility = Visibility.Collapsed;
        }
        String connectionDefault = "Server=localhost;Port=5432;User Id=admin;Password=12345;Database=management;";
        String connectionString;
        NpgsqlConnection npgSqlConnection;
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            LoginGrid.Visibility = Visibility.Collapsed;
            RegistrationGrid.Visibility = Visibility.Visible;
        }
        private async void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Login_Box.Text.Length > 0 && Password_Box.Password.Length > 0)
            {
                LoadGif.Visibility = Visibility.Visible;
                LoginGrid.IsEnabled = false;
                await Task.Run(() =>
                {
                    InitLoad();
                });
                Tasks_DataGrid.ItemsSource = tasksList;
                Clients_DataGrid.ItemsSource = clientsList;
                Workers_DataGrid.ItemsSource = workersList;
                var log = Login_Box.Text;
                var pas = Password_Box.Password;
                await Task.Run(() =>
                {
                    LogIn(log, pas);
                });
                Profile_Name_Label.Content = currentUser.Name;
                Profile_Position_Label.Content = currentUser.Position;
                Profile_Role_Label.Content = currentUser.Role;
                AddTaskClient_ComboBox.ItemsSource = clientsList;
                AddTaskSlave_ComboBox.ItemsSource = workersList;
                WaitToConnect();
            }
            else
                MessageBox.Show("Неверный логин или пароль", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void InitLoad()
        {
            npgSqlConnection = new NpgsqlConnection(connectionDefault);
            npgSqlConnection.Open();
            Thread.Sleep(100);
            workersList = LoadWorkersFromDB();
            Thread.Sleep(100);
            clientsList = LoadClientsFromDB();
            Thread.Sleep(100);
            tasksList = LoadTasksFromDB();
            tasksList.ListChanged += TasksList_ListChanged;
        }
        private void LogIn(string login, string password)
        {
            if (login == "admin")
            {
                currentUser = new WorkerModel() { Role = "Администратор", Id = -1 };
            }
            else
                currentUser = workersList.Where(w => w.Login == login).First();
            if(npgSqlConnection.State == System.Data.ConnectionState.Open)
                npgSqlConnection.Close();
            connectionString = $"Server=localhost;Port=5432;User Id={login.ToLower()};Password={password};Database=management;";
            npgSqlConnection = new NpgsqlConnection(connectionString);
        }
        private async void WaitToConnect()
        {
            try
            {
                await npgSqlConnection.OpenAsync();
                LoginGrid.Visibility = Visibility.Collapsed;
                LoginGrid.IsEnabled = true;
                WorkSheet.Visibility = Visibility.Visible;
                LoadGif.Visibility = Visibility.Collapsed;
            }
            catch
            {
                LoginGrid.IsEnabled = true;
                LoadGif.Visibility = Visibility.Collapsed;
                MessageBox.Show("Не удалось подключиться к базе данных", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private BindingList<ClientModel> LoadClientsFromDB()
        {
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM clients;", npgSqlConnection);
            BindingList<ClientModel> clients = new BindingList<ClientModel>();
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        ClientModel client = new ClientModel()
                        {
                            Id = Convert.ToInt32(reader["client_id"].ToString()),
                            Name = reader["name"].ToString(),
                            Phone = reader["phone_num"].ToString(),
                            Fax = reader["fax_num"].ToString(),
                            Address = reader["address"].ToString(),
                            Email = reader["email"].ToString(),
                            ContactName = reader["contact_name"].ToString()
                        };
                        clients.Add(client);
                    }
                }
            }
            return clients;
        }
        private BindingList<WorkerModel> LoadWorkersFromDB()
        {
            if (npgSqlConnection.State == System.Data.ConnectionState.Closed || npgSqlConnection.State == System.Data.ConnectionState.Connecting) throw new Exception("БД не подключилась");
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM employees;", npgSqlConnection);
            BindingList<WorkerModel> workers = new BindingList<WorkerModel>();
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        WorkerModel worker = new WorkerModel()
                        {
                            Id = Convert.ToInt32(reader["employees_id"].ToString()),
                            Name = reader["name"].ToString(),
                            Position = reader["position"].ToString(),
                            Salary = Convert.ToInt32(reader["salary"].ToString()),
                            Login = reader["login"].ToString()
                        };
                        workers.Add(worker);
                    }
                }
            }
            Thread.Sleep(50);
            var roles = new List<string>();
            string log;
            foreach (WorkerModel worker in workers)
            {
                log = worker.Login;
                NpgsqlCommand command = new NpgsqlCommand($"select rolname from pg_roles where pg_has_role('{log}', oid, 'member');", npgSqlConnection);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            roles.Add(reader.GetString(0));
                        }
                    }
                }
                if (roles.Contains("admin"))
                    worker.Role = "Администратор";
                else if (roles.Contains("worker"))
                    worker.Role = "Работник";
                else if (roles.Contains("manager"))
                    worker.Role = "Менеджер";
            }
            return workers;
        }
        private BindingList<TaskModel> LoadTasksFromDB()
        {
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT task_id, description, client_id, serial_number, creation_date, master_id, priority, slave_id, (extract(epoch from days_to_complete)/86400) as days_left FROM tasks;", npgSqlConnection);
            BindingList<TaskModel> tasks = new BindingList<TaskModel>();
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        TaskModel task = new TaskModel()
                        {
                            Id = Convert.ToInt32(reader["task_id"].ToString()),
                            Description = reader["description"].ToString(),
                            Client = clientsList.Where(c => c.Id == Convert.ToInt32(reader["client_id"].ToString())).First(),
                            Serial_Number = Convert.ToInt32(reader["serial_number"].ToString()),
                            CreationDate = Convert.ToDateTime(reader["creation_date"]),
                            DoUntilDate = Convert.ToDateTime(reader["creation_date"]).AddDays(Convert.ToDouble(reader["days_left"].ToString())),
                            Master = workersList.Where(w => w.Id == Convert.ToInt32(reader["master_id"].ToString())).First(),
                            Priority = reader["priority"].ToString(),
                            Slave = workersList.Where(w => w.Id == Convert.ToInt32(reader["slave_id"].ToString())).First()
                        };
                        tasks.Add(task);
                    }
                }
            }
            return tasks;
        }
        private async void WaitToRegistration(string login, string password, string role)
        {
            try
            {
                LoadGif.Visibility = Visibility.Visible;
                RegistrationGrid.IsEnabled = false;
                await Task.Run(() =>
                {
                    npgSqlConnection.Open();
                    NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"create user {login.ToLower()} encrypted password '{password}';", npgSqlConnection);
                    npgSqlCommand.ExecuteNonQuery();
                    var command = new NpgsqlCommand($"grant {role} to {login.ToLower()};", npgSqlConnection);
                    command.ExecuteNonQuery();
                });
                RegFirstStep_Border.Visibility = Visibility.Collapsed;
                RegSecondStep_Border.Visibility = Visibility.Visible;
                LoadGif.Visibility = Visibility.Collapsed;
                RegistrationGrid.IsEnabled = true;
            }
            catch
            {
                RegistrationGrid.IsEnabled = true;
                LoadGif.Visibility = Visibility.Collapsed;
                MessageBox.Show("Не удалось подключиться к базе данных", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Registration_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Reg_Login_Box.Text.Length < 3)
            {
                MessageBox.Show("Имя пользователя должно содержать более 2 символов", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Reg_Password_Box.Password.Length < 5)
            {
                MessageBox.Show("Пароль должен содержать больше 4 символов", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Reg_Password_Box.Password.Equals(Reg_Password_Repeat_Box.Password))
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (SelectRole_ComboBox.SelectedItem == null)
            {
                MessageBox.Show("Роль не выбрана", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ComboBoxItem role = SelectRole_ComboBox.SelectedItem as ComboBoxItem;
            npgSqlConnection = new NpgsqlConnection(connectionDefault);
            WaitToRegistration(Reg_Login_Box.Text, Reg_Password_Box.Password, role.Tag as string);
        }
        private async void SaveProfileInfo_Button_Click(object sender, RoutedEventArgs e)
        {
            LoadGif.Visibility = Visibility.Visible;
            LoginGrid.IsEnabled = false;

            if (!Regex.IsMatch(Reg_Firstname_Box.Text, @"^[а-яА-Я]+$") || !Regex.IsMatch(Reg_Secondname_Box.Text, @"^[а-яА-Я]+$") || !Regex.IsMatch(Reg_Position_Box.Text, @"^[а-яА-Я ]+$") || (!Regex.IsMatch(Reg_Thirdname_Box.Text, @"^[а-яА-Я]+$") && Reg_Thirdname_Box.Text.Length != 0))
            {
                MessageBox.Show("Поля должны содержать только буквы", "Ошибка формата данных", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Reg_Firstname_Box.Text.Length < 2 || Reg_Secondname_Box.Text.Length < 2 || Reg_Position_Box.Text.Length < 3)
            {
                MessageBox.Show("Не все поля содержат данные", "Пропущены поля", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string fullname = $"{ Reg_Secondname_Box.Text} { Reg_Firstname_Box.Text } { Reg_Thirdname_Box.Text}";
            string position = Reg_Position_Box.Text;
            string login = Reg_Login_Box.Text;
            string password = Reg_Password_Box.Password;
            int salary;
            ComboBoxItem role = SelectRole_ComboBox.SelectedItem as ComboBoxItem;
            if ((string)role.Tag == "worker")
                salary = 40000;
            else
                salary = 70000;
            await Task.Run(() =>
            {
                NpgsqlCommand npgsqlCommand = new NpgsqlCommand($"INSERT INTO employees (name, position,salary, login) VALUES('{fullname}', '{position}', {salary},'{login.ToLower()}');", npgSqlConnection);
                npgsqlCommand.ExecuteNonQuery();
                npgSqlConnection.Close();
                npgSqlConnection.Dispose();
                bool op = true;
            });
            InitLoad();
            Tasks_DataGrid.ItemsSource = tasksList;
            Clients_DataGrid.ItemsSource = clientsList;
            Workers_DataGrid.ItemsSource = workersList;
            var log = Reg_Login_Box.Text;
            var pas = Reg_Password_Box.Password;
            await Task.Run(() =>
            {
                LogIn(log, pas);
            });
            Profile_Name_Label.Content = currentUser.Name;
            Profile_Position_Label.Content = currentUser.Position;
            Profile_Role_Label.Content = currentUser.Role;
            AddTaskClient_ComboBox.ItemsSource = clientsList;
            AddTaskSlave_ComboBox.ItemsSource = workersList;
            WaitToConnect();
            RegistrationGrid.Visibility = Visibility.Collapsed;
            WorkSheet.Visibility = Visibility.Visible;

        }
        private void ListItem_Tasks_Selected(object sender, RoutedEventArgs e)
        {
            Tasks_DataGrid.Visibility = Visibility.Visible;
            Workers_DataGrid.Visibility = Visibility.Hidden;
            Clients_DataGrid.Visibility = Visibility.Hidden;
            AddTask_Grid.Visibility = Visibility.Visible;
            AddTaskClient_ComboBox.ItemsSource = clientsList;
            AddTaskSlave_ComboBox.ItemsSource = workersList;
            Tasks_DataGrid.ItemsSource = tasksList;
        }
        private void ListItem_Clients_Selected(object sender, RoutedEventArgs e)
        {
            Tasks_DataGrid.Visibility = Visibility.Hidden;
            Workers_DataGrid.Visibility = Visibility.Hidden;
            Clients_DataGrid.Visibility = Visibility.Visible;
            AddTask_Grid.Visibility = Visibility.Collapsed;
        }
        private void ListItem_Workers_Selected(object sender, RoutedEventArgs e)
        {
            Tasks_DataGrid.Visibility = Visibility.Hidden;
            Workers_DataGrid.Visibility = Visibility.Visible;
            Clients_DataGrid.Visibility = Visibility.Hidden;
            AddTask_Grid.Visibility = Visibility.Collapsed;
        }
        private async void TasksList_ListChanged(object sender, ListChangedEventArgs e)
        {
            TaskModel model = sender as TaskModel;
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {

            } else if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                LoadGif.Visibility = Visibility.Visible;
                WorkSheet.IsEnabled = false;
                await Task.Run(() =>
                {
                    NpgsqlCommand npgsqlCommand = new NpgsqlCommand("UPDATE tasks SET completion_date = :date WHERE task_id = :id", npgSqlConnection);
                    NpgsqlParameter param1 = new NpgsqlParameter("date", NpgsqlDbType.Timestamp);
                    param1.Value = DateTime.Now;
                    NpgsqlParameter param2 = new NpgsqlParameter("id", NpgsqlDbType.Integer);
                    param2.Value = model.Id;
                    npgsqlCommand.Parameters.Add(param1);
                    npgsqlCommand.Parameters.Add(param2);
                    npgsqlCommand.ExecuteNonQuery();
                });
                WorkSheet.IsEnabled = true;
                LoadGif.Visibility = Visibility.Collapsed;
            }
        }
        private async void AddTask_Button_Click(object sender, RoutedEventArgs e)
        {
            if (AddTaskDescription_TextBox.Text.Length == 0 || AddTaskSerial_TextBox.Text.Length == 0
                || AddTaskDaysToComplete_TextBox.Text.Length == 0 || AddTaskClient_ComboBox.SelectedItem == null
                || AddTaskPriority_ComboBox.SelectedItem == null || AddTaskSlave_ComboBox.SelectedItem == null) return;
            int serial;
            int days;
            if (!Int32.TryParse(AddTaskSerial_TextBox.Text, out serial))
            {
                MessageBox.Show("Cерийный номер должен содержать только цифры", "Ошибка формата данных", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Int32.TryParse(AddTaskDaysToComplete_TextBox.Text, out days))
            {
                MessageBox.Show("Укажите колличество дней цифрами", "Ошибка формата данных", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            LoadGif.Visibility = Visibility.Visible;
            WorkSheet.IsEnabled = false;
            ComboBoxItem boxItem = AddTaskPriority_ComboBox.SelectedItem as ComboBoxItem;
            string priority = boxItem.Tag as string;
            string description = AddTaskDescription_TextBox.Text;
            ClientModel chosenClient = AddTaskClient_ComboBox.SelectedItem as ClientModel;
            WorkerModel chosenSlave = AddTaskSlave_ComboBox.SelectedItem as WorkerModel;
            if(currentUser.Role == "Работник")
            {
                if(currentUser.Id != chosenSlave.Id)
                {
                    WorkSheet.IsEnabled = true;
                    LoadGif.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Вы не можете назначать исполнителем не себя", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            } else if(currentUser.Role == "Менеджер")
            {
                if (currentUser.Id != chosenSlave.Id && chosenSlave.Role == "manager")
                {
                    WorkSheet.IsEnabled = true;
                    LoadGif.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Вы не можете назначать исполнителем начальников отделов", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            await Task.Run(() =>
            {
                NpgsqlCommand npgsqlCommand = new NpgsqlCommand($"INSERT INTO \"tasks\" (description, client_id, serial_number, creation_date, days_to_complete, master_id, priority, slave_id) VALUES('{description}', {chosenClient.Id}, {serial}, :date, :days, {currentUser.Id}, '{priority}', {chosenSlave.Id});", npgSqlConnection);
                NpgsqlParameter param1 = new NpgsqlParameter("date", NpgsqlDbType.Date);
                param1.Value = NpgsqlDate.Now;
                NpgsqlParameter param2 = new NpgsqlParameter("serial_number", NpgsqlDbType.Integer);
                param2.Value = serial;
                NpgsqlTimeSpan timeSpan = NpgsqlTimeSpan.FromDays(days);

                NpgsqlParameter param3 = new NpgsqlParameter("days", NpgsqlDbType.Interval);
                param3.Value = timeSpan;
                npgsqlCommand.Parameters.Add(param1);
                //npgsqlCommand.Parameters.Add(param2);
                npgsqlCommand.Parameters.Add(param3);
                npgsqlCommand.ExecuteNonQuery();
            });
            tasksList = LoadTasksFromDB();
            Tasks_DataGrid.ItemsSource = tasksList;
            WorkSheet.IsEnabled = true;
            LoadGif.Visibility = Visibility.Collapsed;
        }

        #region GifMethods
        //Методы визуализации гифки "Ожидание загрузки"
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _source = GetSource();
            LoadGif.Source = _source;
            ImageAnimator.Animate(_bitmap, OnFrameChanged);
        }
        private void FrameUpdatedCallback()
        {
            ImageAnimator.UpdateFrames();
            if (_source != null)
                _source.Freeze();
            _source = GetSource();
            LoadGif.Source = _source;
            InvalidateVisual();
        }
        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                    new Action(FrameUpdatedCallback));
        }
        Bitmap _bitmap;
        BitmapSource _source;
        private BitmapSource GetSource()
        {
            if (_bitmap == null)
            {
                _bitmap = new Bitmap(@"load.gif");
            }
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = _bitmap.GetHbitmap();
            }
            catch { }
            return Imaging.CreateBitmapSourceFromHBitmap(
                    handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        #endregion 
    }
}
