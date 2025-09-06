using mdk0401_pr1.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Entity;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace mdk0401_pr1
{
    /// <summary>
    /// Логика взаимодействия для PartnerEditWindow.xaml
    /// </summary>
    public partial class PartnerEditWindow : Window
    {
        private PartnerDisplay _editingPartner;
        private bool _isEditMode;

        public PartnerEditWindow()
        {
            InitializeComponent();
            LoadData();
        }

        public PartnerEditWindow(PartnerDisplay partner) : this()
        {
            _editingPartner = partner;
            _isEditMode = true;
            Title = "Редактирование заявки";
            LoadPartnerData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    TypeComboBox.ItemsSource = context.PartnerTypes.ToList();
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPartnerData()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var partner = context.PartnersInfo
                        .Include(p => p.DirectorNames)
                        .Include(p => p.PartnerNames)
                        .FirstOrDefault(p => p.ID == _editingPartner.PartnerId);

                    if (partner == null) return;

                    // Заполняем поля ФИО
                    if (partner.DirectorNames != null)
                    {
                        FamilyNameTextBox.Text = partner.DirectorNames.FamilyName;
                        FirstNameTextBox.Text = partner.DirectorNames.Name;
                        PatronymicTextBox.Text = partner.DirectorNames.Patronymic;
                    }

                    // Заполняем наименование партнера
                    if (partner.PartnerNames != null)
                    {
                        NameTextBox.Text = partner.PartnerNames.Name;
                    }

                    // Остальные поля...
                    TypeComboBox.SelectedValue = partner.IDPartnerType;
                    AddressTextBox.Text = partner.JurAddress;
                    RatingTextBox.Text = partner.Rate.ToString();
                    PhoneTextBox.Text = partner.PhoneNumber;
                    EmailTextBox.Text = partner.Email;
                    InnTextBox.Text = partner.INN; // Добавляем загрузку ИНН
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Валидация обязательных полей
            if (TypeComboBox.SelectedItem == null)
            {
                ShowValidationError("Выберите тип партнера");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowValidationError("Введите наименование партнера");
                return false;
            }

            if (string.IsNullOrWhiteSpace(FamilyNameTextBox.Text))
            {
                ShowValidationError("Введите фамилию директора");
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                ShowValidationError("Введите имя директора");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PatronymicTextBox.Text))
            {
                ShowValidationError("Введите отчество директора");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                ShowValidationError("Введите адрес");
                return false;
            }

            if (!int.TryParse(RatingTextBox.Text, out int rating) || rating < 0)
            {
                ShowValidationError("Рейтинг должен быть целым неотрицательным числом");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                ShowValidationError("Введите телефон");
                return false;
            }

            // Валидация email (если указан)
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) &&
                !IsValidEmail(EmailTextBox.Text))
            {
                ShowValidationError("Введите корректный email адрес");
                return false;
            }

            if (string.IsNullOrWhiteSpace(InnTextBox.Text))
            {
                ShowValidationError("Введите ИНН");
                return false;
            }

            // Проверка формата ИНН (10 или 12 цифр)
            if (!IsValidInn(InnTextBox.Text))
            {
                ShowValidationError("ИНН должен содержать 10 или 12 цифр");
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidInn(string inn)
        {
            // Проверяем что ИНН состоит только из цифр и имеет правильную длину
            return inn.All(char.IsDigit) && (inn.Length == 10 || inn.Length == 12);
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(message, "Ошибка валидации",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void SavePartner()
        {
            using (var context = new ApplicationDbContext())
            {
                PartnersInfo partner;

                if (_isEditMode)
                {
                    // Редактирование существующего партнера
                    partner = context.PartnersInfo
                        .Include(p => p.DirectorNames)
                        .Include(p => p.PartnerNames)
                        .FirstOrDefault(p => p.ID == _editingPartner.PartnerId);

                    if (partner == null)
                    {
                        MessageBox.Show("Партнер не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // Добавление нового партнера
                    partner = new PartnersInfo();
                    context.PartnersInfo.Add(partner);
                }

                // Обновляем сущность
                UpdatePartnerEntity(context, partner);
                context.SaveChanges();
            }
        }

        private void RatingTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void InnTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void UpdatePartnerEntity(ApplicationDbContext context, PartnersInfo partner)
        {
            // Обновляем основные поля
            partner.IDPartnerType = (int)TypeComboBox.SelectedValue;
            partner.JurAddress = AddressTextBox.Text.Trim();
            partner.PhoneNumber = PhoneTextBox.Text.Trim();
            partner.Email = EmailTextBox.Text.Trim();
            partner.INN = InnTextBox.Text.Trim(); // Добавляем сохранение ИНН

            if (int.TryParse(RatingTextBox.Text, out int rating))
            {
                partner.Rate = rating;
            }

            // Обновляем или создаем запись директора
            if (partner.DirectorNames == null)
            {
                partner.DirectorNames = new DirectorNames
                {
                    FamilyName = FamilyNameTextBox.Text.Trim(),
                    Name = FirstNameTextBox.Text.Trim(),
                    Patronymic = PatronymicTextBox.Text.Trim()
                };
            }
            else
            {
                partner.DirectorNames.FamilyName = FamilyNameTextBox.Text.Trim();
                partner.DirectorNames.Name = FirstNameTextBox.Text.Trim();
                partner.DirectorNames.Patronymic = PatronymicTextBox.Text.Trim();
            }

            // Обновляем наименование партнера
            if (partner.PartnerNames == null)
            {
                partner.PartnerNames = new PartnerNames
                {
                    Name = NameTextBox.Text.Trim()
                };
            }
            else
            {
                partner.PartnerNames.Name = NameTextBox.Text.Trim();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                SavePartner();
                DialogResult = true;
                Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                string errorMessage = "Ошибки валидации:\n";

                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessage += $"• {validationError.PropertyName}: {validationError.ErrorMessage}\n";
                    }
                }

                MessageBox.Show(errorMessage, "Ошибки валидации данных",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
