using System.Windows.Input;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class QuickStartComponent : ContentView
{
	public static readonly BindableProperty QuickTrainingCommandProperty = 
		BindableProperty.Create(nameof(QuickTrainingCommand), typeof(ICommand), typeof(QuickStartComponent));

	public static readonly BindableProperty QuickTrainingButtonTextProperty = 
		BindableProperty.Create(nameof(QuickTrainingButtonText), typeof(string), typeof(QuickStartComponent));

	public static readonly BindableProperty ShowHelpCommandProperty = 
		BindableProperty.Create(nameof(ShowHelpCommand), typeof(ICommand), typeof(QuickStartComponent));

	public ICommand QuickTrainingCommand
	{
		get => (ICommand)GetValue(QuickTrainingCommandProperty);
		set => SetValue(QuickTrainingCommandProperty, value);
	}

	public string QuickTrainingButtonText
	{
		get => (string)GetValue(QuickTrainingButtonTextProperty);
		set => SetValue(QuickTrainingButtonTextProperty, value);
	}

	public ICommand ShowHelpCommand
	{
		get => (ICommand)GetValue(ShowHelpCommandProperty);
		set => SetValue(ShowHelpCommandProperty, value);
	}

	public QuickStartComponent()
	{
		InitializeComponent();
		BindingContext = this;
	}
}