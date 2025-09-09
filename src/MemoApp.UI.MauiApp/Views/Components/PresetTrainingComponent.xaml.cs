using System.Collections.ObjectModel;
using System.Windows.Input;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class PresetTrainingComponent : ContentView
{
	public static readonly BindableProperty TrainingOptionsProperty = 
		BindableProperty.Create(nameof(TrainingOptions), typeof(ObservableCollection<TrainingOption>), typeof(PresetTrainingComponent));

	public static readonly BindableProperty StartPresetTrainingCommandProperty = 
		BindableProperty.Create(nameof(StartPresetTrainingCommand), typeof(ICommand), typeof(PresetTrainingComponent));

	public static readonly BindableProperty IsMobileLayoutProperty = 
		BindableProperty.Create(nameof(IsMobileLayout), typeof(bool), typeof(PresetTrainingComponent), defaultValue: false);

	public ObservableCollection<TrainingOption> TrainingOptions
	{
		get => (ObservableCollection<TrainingOption>)GetValue(TrainingOptionsProperty);
		set => SetValue(TrainingOptionsProperty, value);
	}

	public ICommand StartPresetTrainingCommand
	{
		get => (ICommand)GetValue(StartPresetTrainingCommandProperty);
		set => SetValue(StartPresetTrainingCommandProperty, value);
	}

	public bool IsMobileLayout
	{
		get => (bool)GetValue(IsMobileLayoutProperty);
		set => SetValue(IsMobileLayoutProperty, value);
	}

	public PresetTrainingComponent()
	{
		InitializeComponent();
		// Don't set BindingContext - inherit from parent
	}
}