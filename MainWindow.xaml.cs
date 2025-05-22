using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Collections.Generic;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SymlinkApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            string originalPath = folder1.Text; //Cesta, kde je slozka/soubor ale bude tam potom symlink
            string targetPath = folder2.Text; //Cesta, kam se původní složka/soubor zkopíruje

            FileAttributes attr = File.GetAttributes(originalPath);
            var soubor = File.Exists(originalPath);
            var slozka = Directory.Exists(originalPath);

            bool boolSlozka = Boolean.TryParse(slozka.ToString(), out bool boolslozka);

            List<string> symlinkPath = new List<string>();
            symlinkPath.Add("mklink");

            if (File.Exists(originalPath) || Directory.Exists(originalPath))
            {
                if ((attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    // Je to symlink – smažem podle typu
                    var result = MessageBox.Show("Bacha, chystám se smazat symlink, vážně to chceš udělat kašpare?", "Delete symlink", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (Directory.Exists(originalPath))
                            Directory.Delete(originalPath, true);
                        else
                            File.Delete(originalPath);
                    }
                    
                }
                else if (slozka)
                {
                    var result = MessageBox.Show($"Budu přesouvat složku {originalPath} \n Chceš pokračovat ?", "DeleteFolder", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.Delete(originalPath, true);
                    }
                    else
                        return;
                }
                else if (soubor)
                {
                    var result = MessageBox.Show($"Chystám se smazat soubor {originalPath} co ale není symlink. \n Chceš pokračovat ?", "DeleteFile", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        File.Delete(originalPath);
                    }
                    else
                        return;
                }
            }

            if (slozka)
            {
                symlinkPath.Add("/D");
            }
            symlinkPath.Add($"\"{originalPath}\"");
            symlinkPath.Add($"\"{targetPath}\"");
            string createSymlinkPath = string.Join(" ", symlinkPath);

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",$"/k {createSymlinkPath}");
            psi.UseShellExecute = true;
            psi.Verb = "runas";
            Process proc = Process.Start(psi);

            try
            {
                if (proc != null)
                {
                    
                }
                else
                {
                    // Toto by se stalo, jen kdyby Process.Start samo vrátilo null, což je velmi vzácné.
                    MessageBox.Show("Nepodařilo se získat referenci na spuštěný CMD proces.", "Chyba spuštění", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.ComponentModel.Win32Exception exUac) when (exUac.NativeErrorCode == 1223) // Uživatel zrušil UAC
            {
                MessageBox.Show("Vytváření symlinku zrušeno uživatelem (UAC). CMD okno se nespustilo.", "Zrušeno", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception exSymlink) // Jiné chyby při pokusu o SP mischievousnessUŠTĚNÍ procesu
            {
                MessageBox.Show($"Chyba při pokusu o spuštění CMD pro mklink: {exSymlink.Message}", "Chyba spuštění", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                runBtn_Click(null, null);
            }
        }

        private void btnBrowse1_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folder1.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnBrowse2_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folder2.Text = dialog.SelectedPath;
                }
            }
        }

        private void delBtn_Click(object sender, RoutedEventArgs e)
        {
            string originalPath = folder1.Text;
            string targetPath = folder2.Text;

            FileAttributes attr = File.GetAttributes(originalPath);
            var soubor = File.Exists(originalPath);
            var slozka = Directory.Exists(originalPath);

            if ((attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                // Je to symlink – smažem podle typu
                var result2 = MessageBox.Show("Bacha, chystám se smazat symlink, vážně to chceš udělat kašpare?", "Delete symlink", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result2 == MessageBoxResult.Yes)
                {
                    if (Directory.Exists(originalPath))
                        Directory.Delete(originalPath);
                    else
                        File.Delete(originalPath);
                }

            }
            else if (slozka)
            {
                var result2 = MessageBox.Show($"Chystám se smazat složku {originalPath}", "DeleteFolder", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result2 == MessageBoxResult.Yes)
                {
                    Directory.Delete(originalPath, true);
                }
                else
                    return;
            }
            else if (soubor)
            {
                var result2 = MessageBox.Show($"Chystám se smazat soubor {originalPath} co ale není symlink", "DeleteFile", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result2 == MessageBoxResult.Yes)
                {
                    File.Delete(originalPath);
                }
                else
                    return;
            }
        

            if (!slozka && !soubor)
            {
                MessageBox.Show($"Cesta '{originalPath}' neexistuje, more šašku, nemám co smazat.", "Cesta nenalezena", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            
            try
            {
                if (slozka)
                {
                    Directory.Delete(originalPath, true);
                    MessageBox.Show("Složka smazána", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                else if (soubor)
                {
                    File.Delete(originalPath);
                    MessageBox.Show("Soubor smazán", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
                }

            }
            catch (UnauthorizedAccessException)
            {
                var elevateResult = MessageBox.Show(
                    $"Máš malý práva na to smazat {originalPath}.\n\nChceš zkusit to zkusit smazat s admin právama?",
                    "Vyžadována administrátorská práva",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (elevateResult == MessageBoxResult.Yes)
                {
                    TryDeleteWithAdminRights(originalPath, slozka);
                }

            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Chyba při mazání (I/O): {ioEx.Message}\nUjisti se, že soubor nebo složka není používána jiným programem.", "Chyba při mazání", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Obecná chyba při mazání: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TryDeleteWithAdminRights(string pathToDelete, bool isDirectory)
        {
            try
            {
                string cmdCommand;
                if (isDirectory)
                {
                    // rd (smaže složku) /s (smaže strom adresářů) /q (tichý mód, bez potvrzení)
                    cmdCommand = $"/C rd /s /q \"{pathToDelete}\"";
                }
                else // je to soubor
                {
                    // del (smaže soubor) /f (vynutí smazání read-only souborů) /q (tichý mód)
                    cmdCommand = $"/C del /f /q \"{pathToDelete}\"";
                }

                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmdCommand) //spusti cmd jako admin
                {
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = true,
                };

                Process proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(10000); // Počkej max 10 sekund, abys nezablokoval GUI na dlouho

                    // Kontrola, zda soubor/složka stále existuje, může být zavádějící,
                    // protože i když cmd vrátí chybu, nemusí to znamenat, že se nic nesmazalo.
                    if (!proc.HasExited)
                    {
                        MessageBox.Show($"Pokus o smazání '{pathToDelete}' s admin právy stále běží. Zkontroluj úspěšnost smazání pozdějc.", "Mazání probíhá", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (proc.ExitCode == 0)
                    {
                        MessageBox.Show($"'{pathToDelete}' bylo úspěšně smazáno s admin právy.", "Smazáno s admin právy", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Ověření, zda cesta stále existuje
                        bool stillExists = isDirectory ? Directory.Exists(pathToDelete) : File.Exists(pathToDelete);
                        if (!stillExists)
                        {
                            MessageBox.Show($"'{pathToDelete}' bylo smazáno s administrátorskými právy (i přes chybu cmd {proc.ExitCode}).", "Smazáno s admin právy", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Při pokusu o smazání '{pathToDelete}' s admin právy došlo k chybě (cmd exit code: {proc.ExitCode}). Zkontroluj oprávnění a zda není soubor/složka používána.", "Chyba při elevovaném mazání", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Nepodařilo se spustit proces pro smazání s admin právy.", "Chyba spuštění", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.ComponentModel.Win32Exception ex) // Zachytí, pokud uživatel zruší UAC dialog (kód 1223)
            {
                if (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
                {
                    MessageBox.Show("Operace smazání s admin právy byla zrušena uživatelem (UAC).", "Zrušeno", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Chyba při pokusu o smazání s admin právy (Win32): " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex_elevated)
            {
                MessageBox.Show("Obecná chyba při pokusu o smazání s admin právy: " + ex_elevated.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}