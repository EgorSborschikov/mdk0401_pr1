using mdk0401_pr1.Entities;
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

namespace mdk0401_pr1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        //Метод загрузки данных на страницу из базы данных
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPartners();
        }

        private void LoadPartners()
        {
            try
            {
                // Подключение к базе данных, запрос на вытягивание данных 
                using (var context = new ApplicationDbContext())
                {
                    Console.WriteLine("Подключение к БД...");

                    var partners = context.PartnersInfo.ToList();
                    var partnerNames = context.PartnerNames.ToList();
                    var partnerTypes = context.PartnerTypes.ToList();
                    var directorNames = context.DirectorNames.ToList();
                    var partnerProducts = context.PartnersProducts.ToList();
                    var products = context.Products.ToList();

                    Console.WriteLine($"Найдено партнеров: {partners.Count}");
                    Console.WriteLine($"Найдено продуктов: {products.Count}");

                    // Список для отображения ланных
                    var partnerList = new List<PartnerDisplay>();

                    foreach (var partner in partners)
                    {
                        var partnerName = partnerNames.FirstOrDefault(pn => pn.ID == partner.IDPartnerName);
                        var partnerType = partnerTypes.FirstOrDefault(pt => pt.ID == partner.IDPartnerType);
                        var directorName = directorNames.FirstOrDefault(dn => dn.ID == partner.IDDirectorName);

                        // Поиск продуктов, предоставляемых поставщиком
                        var productsForPartner = partnerProducts
                            .Where(pp => pp.IDPartner == partner.ID)
                            .ToList();

                        // Получаем информацию о продуктах
                        var productDetails = new List<ProductDisplay>();
                        decimal totalCost = 0;

                        foreach (var partnerProduct in productsForPartner)
                        {
                            var product = products.FirstOrDefault(p => p.ID == partnerProduct.IDProduct);
                            if (product != null)
                            {
                                decimal productCost = partnerProduct.ProductAmount * product.MinCost;
                                decimal discount = CalculateDiscount(partner.Rate);
                                productCost = productCost * (1 - discount);

                                if (productCost < 0) productCost = 0;

                                productDetails.Add(new ProductDisplay
                                {
                                    Name = product.ProductionName,
                                    Amount = partnerProduct.ProductAmount,
                                    Cost = Math.Round(productCost, 2)
                                });

                                totalCost += productCost;
                            }
                        }

                        partnerList.Add(new PartnerDisplay
                        {
                            PartnerId = partner.ID,
                            Type = partner.PartnerTypes?.Type ?? "Не указано",
                            Name = partnerName?.Name ?? "Не указано",
                            Address = partner.JurAddress,
                            Phone = partner.PhoneNumber,
                            Rate = partner.Rate,
                            Director = directorName?.Name ?? "Не указано",
                            TotalCost = Math.Round(totalCost, 2),
                            Products = productDetails
                        });
                    }

                    // Установка данных в ListBox
                    PartnersListBox.ItemsSource = partnerList;
                    Console.WriteLine($"Загружено заявок: {partnerList.Count}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void PartnerItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is PartnerDisplay partner)
                {
                    var editWindow = new PartnerEditWindow(partner);
                    if (editWindow.ShowDialog() == true)
                    {
                        LoadPartners(); // Обновляем список после редактирования
                        ShowInfo("Успешно", "Заявка успешно обновлена");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка", ex.Message);
            }
        }

        // Метод расчета скидки
        private decimal CalculateDiscount(int rate)
        {
            return Math.Min(rate / 2 * 0.01m, 0.15m);
        }

        // Обработчик события редактирования заявки
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new PartnerEditWindow();

                if (editWindow.ShowDialog() == true)
                {
                    LoadPartners();
                    ShowInfo("Успешно!", "Заявка успешно обновлена");
                }
            } catch (Exception ex)
            {
                ShowError("Ошибка", ex.Message);
            }
        }

        // Методы с шаблоном отображения всплывающих окон
        private void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfo(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Дополнительные классы модели представления
    public class PartnerDisplay
    {
        public int PartnerId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int Rate { get; set; }
        public string Director { get; set; }
        public decimal TotalCost { get; set; }
        public List<ProductDisplay> Products { get; set; }
    }

    public class ProductDisplay
    {
        public string Name { get; set; }
        public int Amount { get; set; }
        public decimal Cost { get; set; }
    }
}
