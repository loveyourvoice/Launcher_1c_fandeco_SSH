using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Update1C
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                DialogResult result = MessageBox.Show("Ошибка: Приложение должно быть запущено с аргументами!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //SSH подключение
            string sshHost = "0.0.0.1";
            int sshPort = 123;
            string sshUser = "123";
            string sshPassword = "123";
            string server = "/1C-BIN/"; // Путь на SSH сервере, где хранятся версии 1С



            string argumentsFor1C = string.Join(" ", args.Skip(1).ToArray());
            string folderName = args[0];
            //string server = @"\\SrvStart1C\1C-BIN\";
            string fullpath = Path.Combine(server, folderName);
            string local = @"C:\=FANDECO=\1C-BIN\";
            string oldFolder = Path.Combine(local, folderName + "_old");
            string newFolder = Path.Combine(local, folderName + "_new");
            string lockFile = Path.Combine(local, folderName + ".lock");
            string folderPath = Path.Combine(local, folderName);
            string server1c = Path.Combine(server, folderName, "1cv8c.exe");
            string local1c = Path.Combine(local, folderName, "1cv8c.exe");
            string serverPath = Path.Combine(server, folderName);
            bool needToUpdate = false;

            try
            {
                using (var client = new SftpClient(sshHost, sshPort, sshUser, sshPassword))
                {
                    client.Connect();

                    // Путь к 1cv8c.exe на сервере
                    string remoteFilePath = server + folderName + "/1cv8c.exe";

                    // Проверяем существование файла
                    bool remoteFileExists = client.Exists(remoteFilePath);
                    if (remoteFileExists)
                    {

                        if (!File.Exists(local1c))
                        {
                            needToUpdate = true;
                        }
                        if (File.Exists(local1c))
                        {

                            var serverFile = client.Get(remoteFilePath);

                            var serverFileInfo = serverFile.Attributes.Size;
                            var localFileInfo = new FileInfo(local1c);

                            // Добавляем отладочные сообщения
                           /* MessageBox.Show($"Размер локальной 1С: {localFileInfo.Length}\nРазмер 1С на сервере: {serverFileInfo}", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
*/
                            if (localFileInfo.Length != serverFileInfo)
                            {
                                // Вызов функции обновления

                                needToUpdate = true;
                                /*MessageBox.Show("Версии разные, обновление требуется", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);*/
                            }
                            else
                            {
                                /*MessageBox.Show("Версии одинаковые, обновление не требуется", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);*/
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Файл 1cv8c.exe не найден на сервере.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            if (!(Directory.Exists(fullpath)))
            {
                server1c = Path.Combine(server, folderName, "1cv8c.exe");
                serverPath = Path.Combine(server, folderName);
            }

            if (!Directory.Exists(@"C:\=FANDECO=\"))
            {
                Directory.CreateDirectory(@"C:\=FANDECO=\");
            }

            if (!Directory.Exists(@"C:\=FANDECO=\1C-BIN"))
            {
                Directory.CreateDirectory(@"C:\=FANDECO=\1C-BIN");
            }

            if (File.Exists(lockFile))
            {
                if (Directory.Exists(newFolder))
                {
                    Directory.Delete(newFolder, true);
                }

                if (Directory.Exists(oldFolder))
                {
                    Directory.Delete(oldFolder, true);
                }
            }
          

            if (needToUpdate) // до этого было несколько проверок на необходимость обновления, если необходимость обновления была выявлена, то работает дальнейший код
            {
                try // пробую переименовать
                {
                    if (Directory.Exists(folderPath)) // если оригинальная папка существует
                    {
                        Directory.Move(folderPath, oldFolder); // переименовать, добавив приписку _old
                    }
                }
                catch // если не получилось
                {
                    DialogResult result = MessageBox.Show("Закройте все 1С, нажмите ОК и запустите программу заново.\r\n" +
                        "Если вы уверены, что все 1С закрыты, а ошибка остатется - перезагрузите компьютер.", "Ошибка при обновлении", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
                if (!Directory.Exists(newFolder)) // если не существует дириктории с припиской base_new
                {
                    Directory.CreateDirectory(newFolder); // то создать ее
                }

                if (!File.Exists(lockFile)) // если не существует файла .lock
                {
                    File.Create(lockFile).Close(); // то создать и закрыть его в текущем потоке
                }
                Thread thread = null;
                thread = new Thread(() =>  // запуск потока для отображения модального окна
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Form form = new Form();
                    System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar();
                    progressBar.Value = 0;
                    progressBar.Width = 650;
                    progressBar.Height = 20;
                    progressBar.Maximum = 1000;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Left = 0;
                    progressBar.Top = 110;
                    form.Controls.Add(progressBar);
                    form.Width = 650;
                    form.Height = 180;
                    form.Font = new System.Drawing.Font("Tahoma", 11);
                    form.Text = "Обновление 1С";
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    form.ControlBox = false;
                    form.ShowInTaskbar = true;
                    form.StartPosition = FormStartPosition.CenterScreen;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.TopMost = true;
                    Label label = new Label();
                    label.Text = "Изменилась версия программного обеспечения сервера 1С:Предприятия.\r\n";
                    label.Text += "Выполняется обновление клиентской части 1С на вашем компьютере.\r\n";
                    label.Text += "Дождитесь завершения процесса.\r\n";
                    label.Text += "По завершении программа запустится сама.";
                    label.Width = form.Width;
                    label.Height = 80;
                    label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    label.ForeColor = System.Drawing.Color.Black;
                    form.Controls.Add(label);
                    Label label2 = new Label();
                    label2.Text = "® TechnoLight";
                    label2.Width = form.Width;
                    label2.Height = 80;
                    label2.TextAlign = System.Drawing.ContentAlignment.BottomRight;
                    label2.ForeColor = System.Drawing.Color.OrangeRed;
                    label2.Top = 22;
                    label2.Left = -18;
                    form.Controls.Add(label2);
                    Label label3 = new Label();
                    label3.Text = "Начинаю обновление...";
                    label3.Width = 370;
                    label3.Height = 20;
                    label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
                    label3.ForeColor = System.Drawing.Color.DimGray;
                    label3.Top = 85;
                    label3.Left = 10;
                    form.Controls.Add(label3);
                    label2.SendToBack();
                    label3.BringToFront();
                    form.Load += (sender, arguments) =>
                    {
                        var uiContext = SynchronizationContext.Current;
                        Thread updateThread = new Thread(() =>
                        {
                            while (true)
                            {
                                if (File.Exists(lockFile))
                                {
                                    try
                                    {
                                        using (var fileStream = new FileStream(lockFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                        {
                                            var newProgressLines = File.ReadAllLines(lockFile);
                                            if (newProgressLines.Length > 1)
                                            {
                                                var newProgressCopy = int.Parse(newProgressLines.FirstOrDefault());
                                                var progresscopy = Math.Min(newProgressCopy, 1000);
                                                uiContext.Send(state => { progressBar.Value = progresscopy; }, null);
                                                if (progresscopy < 1000)
                                                uiContext.Send(state => { label3.Text = newProgressLines[1]; }, null);
                                                if (progresscopy == 1000) {
                                                    uiContext.Send(state => { label3.Text = "Подготовка к запуску 1С"; }, null);
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        continue;
                                    };
                                }
                                else
                                {
                                    continue;
                                }
                                Thread.Sleep(20);
                            }
                        });
                        updateThread.Start();
                    };
                    Application.Run(form);
                });
                thread.Start(); // выше код модального окна, данная строка запускает его

                if (Directory.Exists(oldFolder)) // если существует папка с припиской _old 
                {
                    Directory.Delete(oldFolder, true); // удалить ее
                }
                using (var client = new SftpClient(sshHost, sshPort, sshUser, sshPassword))
                {
                    client.Connect();
                    DirectoryCopy(client, serverPath, newFolder, true, true, folderName); // запускаем поток обновления (копирования) файлов      !!!(вынесено отдельной функцией, код ниже)!!!
                }
                if (File.Exists(lockFile)) // если файл lock существует после обновления
                {
                    File.Delete(lockFile); // удаляем файл lock после завершения обновления
                }

                if (Directory.Exists(oldFolder)) // если существует папка с припиской _old 
                {
                    Directory.Delete(oldFolder, true); // удалить ее
                }

                Directory.Move(newFolder, folderPath);// переименовываем папку _new в оригинальную
                thread.Abort(); // закрывае поток модального окна 

            }

            string exePath = Path.Combine(folderPath, "1cv8c.exe");
            if (File.Exists(exePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exePath;
                startInfo.Arguments = argumentsFor1C;
                System.Diagnostics.Process.Start(startInfo);
                Environment.Exit(0);
            }
            else
            {
                DialogResult result = MessageBox.Show("Файл 1cv8c.exe не найден, обратитесь к системному администратору...",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }
        static void DirectoryCopy(SftpClient client, string sourceDirPath, string destDirPath, bool copySubDirs, bool createlock, string folderName)
        {
            try
            {
                // Получаем содержимое директории на сервере
                var sourceFiles = client.ListDirectory(sourceDirPath);

                if (!Directory.Exists(destDirPath))
                {
                    Directory.CreateDirectory(destDirPath);
                }

                long totalSize = sourceFiles.Sum(f => f.Length);
                int copiedFiles = 0;
                long copiedSize = 0;

                foreach (var sourceFile in sourceFiles)
                {
                    if (!sourceFile.IsDirectory)
                    {
                        // Получаем информацию о файле на сервере
                        var remoteFilePath = sourceFile.FullName;
                        var localFilePath = Path.Combine(destDirPath, sourceFile.Name);

                        // Скачиваем файл с сервера
                        using (var fileStream = File.Create(localFilePath))
                        {
                            client.DownloadFile(remoteFilePath, fileStream);
                        }

                        copiedFiles++;
                        copiedSize += sourceFile.Length;

                        try
                        {
                            if (createlock)
                            {
                                int percentage = (int)Math.Round((double)copiedSize / totalSize * 1000);
                                File.WriteAllText(@"C:\=FANDECO=\1C-BIN\" + folderName + ".lock", $"{percentage}\r\n{folderName + "/" + sourceFile.Name}");
                            }
                        }
                        catch { continue; }
                    }
                }

                if (copySubDirs)
                {
                    foreach (var sourceSubDir in sourceFiles.Where(f => f.IsDirectory && f.Name != "." && f.Name != ".."))
                    {
                        string sourceSubDirPath = sourceSubDir.FullName;
                        string destSubDirPath = Path.Combine(destDirPath, sourceSubDir.Name);

                        // Переходим в поддиректорию на сервере
                        client.ChangeDirectory(sourceSubDirPath);

                        DirectoryCopy(client, sourceSubDirPath, destSubDirPath, true, false, folderName);  // Передаем параметры рекурсивно

                        // Возвращаемся обратно после завершения рекурсии
                     
                    }
                }
            }
            catch (Exception ex)
            {
                DialogResult result = MessageBox.Show("Ошибка при работе с папкой " + sourceDirPath + ": " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}