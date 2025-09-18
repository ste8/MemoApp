using Microsoft.Maui.Controls;
using System.Collections;

namespace MemoApp.UI.MauiApp.Controls
{
    /// <summary>
    /// A modern, beautiful picker control with icon support and smooth animations
    /// </summary>
    public partial class ModernPicker : ContentView
    {
        public ModernPicker()
        {
            InitializeComponent();
        }

        #region Bindable Properties

        // ItemsSource
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IList),
                typeof(ModernPicker),
                null,
                propertyChanged: OnItemsSourceChanged);

        public IList ItemsSource
        {
            get => (IList)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        // SelectedItem
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(
                nameof(SelectedItem),
                typeof(object),
                typeof(ModernPicker),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedItemChanged);

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        // SelectedIndex
        public static readonly BindableProperty SelectedIndexProperty =
            BindableProperty.Create(
                nameof(SelectedIndex),
                typeof(int),
                typeof(ModernPicker),
                -1,
                BindingMode.TwoWay);

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        // ItemDisplayBinding
        public static readonly BindableProperty ItemDisplayBindingProperty =
            BindableProperty.Create(
                nameof(ItemDisplayBinding),
                typeof(BindingBase),
                typeof(ModernPicker),
                null);

        public BindingBase ItemDisplayBinding
        {
            get => (BindingBase)GetValue(ItemDisplayBindingProperty);
            set => SetValue(ItemDisplayBindingProperty, value);
        }

        // Title (Placeholder)
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(
                nameof(Title),
                typeof(string),
                typeof(ModernPicker),
                string.Empty);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Icon
        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(
                nameof(Icon),
                typeof(string),
                typeof(ModernPicker),
                string.Empty,
                propertyChanged: OnIconChanged);

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // IconColor
        public static readonly BindableProperty IconColorProperty =
            BindableProperty.Create(
                nameof(IconColor),
                typeof(Color),
                typeof(ModernPicker),
                Application.Current.Resources["Primary"] as Color);

        public Color IconColor
        {
            get => (Color)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }

        // IconFontFamily
        public static readonly BindableProperty IconFontFamilyProperty =
            BindableProperty.Create(
                nameof(IconFontFamily),
                typeof(string),
                typeof(ModernPicker),
                "MaterialIcons");

        public string IconFontFamily
        {
            get => (string)GetValue(IconFontFamilyProperty);
            set => SetValue(IconFontFamilyProperty, value);
        }

        // HasIcon (Read-only)
        public static readonly BindableProperty HasIconProperty =
            BindableProperty.Create(
                nameof(HasIcon),
                typeof(bool),
                typeof(ModernPicker),
                false);

        public bool HasIcon
        {
            get => (bool)GetValue(HasIconProperty);
            private set => SetValue(HasIconProperty, value);
        }

        // BorderStyle
        public static readonly BindableProperty BorderStyleProperty =
            BindableProperty.Create(
                nameof(BorderStyle),
                typeof(Style),
                typeof(ModernPicker),
                null,
                propertyChanged: OnBorderStyleChanged);

        public Style BorderStyle
        {
            get => (Style)GetValue(BorderStyleProperty);
            set => SetValue(BorderStyleProperty, value);
        }

        #endregion

        #region Property Changed Handlers

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ModernPicker control && control.PickerControl != null)
            {
                control.PickerControl.ItemsSource = newValue as IList;
            }
        }

        private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ModernPicker control && control.PickerControl != null)
            {
                control.PickerControl.SelectedItem = newValue;
            }
        }

        private static void OnIconChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ModernPicker control)
            {
                control.HasIcon = !string.IsNullOrEmpty(newValue as string);
            }
        }

        private static void OnBorderStyleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ModernPicker control && control.PickerBorder != null && newValue is Style style)
            {
                control.PickerBorder.Style = style;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Focus the picker control
        /// </summary>
        public new void Focus()
        {
            PickerControl?.Focus();
        }

        /// <summary>
        /// Unfocus the picker control
        /// </summary>
        public new void Unfocus()
        {
            PickerControl?.Unfocus();
        }

        #endregion
    }
}