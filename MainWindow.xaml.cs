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

            CopyFolderContents(sender, e); //kopíruje obsah složky (soubory) z originalPath do targetPath
            CopyDirectory(originalPath, targetPath, true); //kopíruje obsah složky (podsložky) z originalPath do targetPath
            delBtn_Click(sender, e); //mazání původní složky/souboru/symlinku (originalPath)

            List<string> symlinkPath = new List<string>();
            symlinkPath.Add("mklink");

            if (slozka)
            {
                symlinkPath.Add("/D"); // pro symlink na složku
            }

            symlinkPath.Add($"\"{originalPath}\"");
            symlinkPath.Add($"\"{targetPath}\"");
            string createSymlinkPath = string.Join(" ", symlinkPath); //complete command for making symlink in cmd

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",$"/k {createSymlinkPath}"); //spusti cmd jako admin
            psi.UseShellExecute = true;
            psi.Verb = "runas";  //spusti cmd jako admin
            Process proc = Process.Start(psi);

            #region Chyby při spuštění CMD
            try
            {
                if (proc == null)
                {
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
            #endregion
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e) //Enter key starts runBtn_Click
        {
            if (e.Key == Key.Enter)
            {
                runBtn_Click(null, null);
            }
        }

        private void btnBrowse1_Click(object sender, RoutedEventArgs e) //route for originalPath
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    folder1.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnBrowse2_Click(object sender, RoutedEventArgs e) //route for targetPath
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
            string originalPath = folder1.Text.Trim();

            if (string.IsNullOrWhiteSpace(originalPath))
            {
                MessageBox.Show("Není zadaná zdrojová cesta.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            #region Definice proměnných
            bool isDirectory;
            bool isFile;
            bool isSymlink = false;
            FileAttributes attributes = 0;

            try
            {
                if (File.Exists(originalPath) || Directory.Exists(originalPath))
                {
                    attributes = File.GetAttributes(originalPath);
                    isSymlink = (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

                    if (isSymlink)
                    {
                        isDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
                        isFile = !isDirectory;
                    }
                    else
                    {
                        isDirectory = Directory.Exists(originalPath); 
                        isFile = File.Exists(originalPath);
                    }
                }
                else
                {
                    MessageBox.Show($"Cesta '{originalPath}' neexistuje.", "Cesta nenalezena", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch (Exception exAttr) 
            {
                MessageBox.Show($"Chyba při čtení informací o cestě '{originalPath}': {exAttr.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            #endregion

            #region Potvrzení smazání
            string typPolozky = isSymlink ? "symlink" : (isDirectory ? "složku" : "soubor");
            string zpravaPotvrzeni;

            if (isSymlink)
            {
                zpravaPotvrzeni = $"Opravdu chcete smazat {typPolozky} '{originalPath}'?";
            }
            else
            {
                zpravaPotvrzeni = $"Opravdu chcete smazat {(isDirectory ? "složku a VŠECHEN její obsah" : "soubor")} '{originalPath}'?";
            }

            var confirmResult = MessageBox.Show(zpravaPotvrzeni, "Potvrzení smazání", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmResult == MessageBoxResult.No)
            {
                MessageBox.Show("Mazání zrušeno uživatelem.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            #endregion

            try
            {
                if (isSymlink)
                {
                    if (isDirectory) // Symlink na adresář
                        Directory.Delete(originalPath, false); // false, protože mažeme jen samotný odkaz
                    else // Symlink na soubor
                        File.Delete(originalPath);
                    MessageBox.Show($"Symlink '{originalPath}' smazán.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (isDirectory)
                {
                    Directory.Delete(originalPath, true); // Smazání skutečné složky
                    MessageBox.Show($"Složka '{originalPath}' a její obsah smazány.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (isFile)
                {
                    File.Delete(originalPath); // Smazání skutečného souboru
                    MessageBox.Show($"Soubor '{originalPath}' smazán.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
            }
            catch (UnauthorizedAccessException)
            {
                var elevateResult = MessageBox.Show(
                    $"Nemáte oprávnění smazat '{originalPath}'.\nChcete to zkusit jako administrátor?",
                    "Vyžadována administrátorská práva",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (elevateResult == MessageBoxResult.Yes)
                {
                    TryDeleteWithAdminRights(originalPath, isDirectory);
                }
            }
            catch (IOException ioEx)
            {
                string itemDesc = isSymlink ? "symlink" : (isDirectory ? "složku" : "soubor");
                MessageBox.Show($"Chyba při mazání {itemDesc} \nChyba: {ioEx.Message}\nUjistěte se, že {itemDesc} není používána jiným programem.", "Chyba při mazání", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Obecná chyba při mazání '{originalPath}': {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
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
                else
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
        private void CopyFolderContents(object sender, RoutedEventArgs e) //kopíruje soubory do cílové složky
        {
            string sourceFolderPath = folder1.Text.Trim();
            string destinationFolderPath = folder2.Text.Trim();

            if (string.IsNullOrWhiteSpace(sourceFolderPath) || string.IsNullOrWhiteSpace(destinationFolderPath))
            {
                MessageBox.Show("Nezadals zdrojovou nebo cílovou cestu more šašku.", "Chybějící cesty", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(sourceFolderPath))
            {
                MessageBox.Show($"Zdrojová složka '{sourceFolderPath}' neexistuje.", "Zdroj nenalezen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Cílová složka nemusí existovat, bude vytvořena.
            // Ale pokud v cíli existuje soubor se stejným jménem jako cílová složka, je to problém.
            if (File.Exists(destinationFolderPath))
            {
                MessageBox.Show($"Cílová cesta '{destinationFolderPath}' již existuje jako soubor. Zadej platnej název pro cílovou složku more jinak si tě najdu.", "Konflikt v cíli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var confirmResult = MessageBox.Show(
                    $"Opravdu chceš zkopírovat VEŠKERÝ OBSAH ze složky:\n'{sourceFolderPath}'\n\ndo složky:\n'{destinationFolderPath}'?\n\nPokud v cílové složce existují soubory se stejným názvem, budou PŘEPSÁNY.",
                    "Potvrdit kopírování obsahu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    CopyDirectory(sourceFolderPath, destinationFolderPath, true); // true pro přepsání existujících souborů
                    MessageBox.Show("Obsah složky byl úspěšně zkopírován.", "Kopírování dokončeno", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Kopírování zrušeno.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (DirectoryNotFoundException dirEx)
            {
                MessageBox.Show($"Chyba: Zdrojový nebo část cílového adresáře nebyla nalezena.\n{dirEx.Message}", "Chyba adresáře", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Chyba během kopírování: {ioEx.Message}\nUjistěte se, že soubory nejsou používány, máte dostatek místa na disku a platné názvy cest.", "Chyba I/O", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                var result = MessageBox.Show($"Nedostatečná oprávnění pro přístup k souborům/složkám: {uaEx.Message}\nChceš ji zkusit smazat s admin právama ?", "Chyba oprávnění", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    TryDeleteWithAdminRights(sourceFolderPath, true); // Pokus o smazání s admin právy
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Neočekávaná chyba při kopírování: {ex.Message}", "Obecná chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static void CopyDirectory(string sourceDirPath, string destDirPath, bool overwriteFiles = true)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirPath);
            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"Zdrojový adresář nebyl nalezen: {sourceDirPath}");
            }

            // Pokud cílový adresář neexistuje, vytvoříme ho.
            if (!Directory.Exists(destDirPath))
            {
                Directory.CreateDirectory(destDirPath);
            }

            // Zkopírujeme všechny soubory v aktuálním adresáři.
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDirPath, file.Name);
                file.CopyTo(targetFilePath, overwriteFiles);
            }

            // Zkopírujeme všechny podsložky (rekurzivně).
            foreach (DirectoryInfo subDir in sourceDir.GetDirectories())
            {
                string newDestSubDirPath = Path.Combine(destDirPath, subDir.Name);
                CopyDirectory(subDir.FullName, newDestSubDirPath, overwriteFiles);
            }
        }
    }
}