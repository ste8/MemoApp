**_Created as an experiment for Vibe Coding_**

# MemoApp - Mnemonic Major System Training

A progressive training game for the **Mnemonic Major System** - a powerful memory technique that associates numbers (0-99) with memorable words or images.



## What is the Major System?

The Major System is a mnemonic technique for memorizing numbers by converting them into consonant sounds, which then form words or images. Each digit (0-9) corresponds to specific consonant sounds:

- **0** → S, soft C, Z sounds
- **1** → T, D sounds  
- **2** → N sound
- **3** → M sound
- **4** → R sound
- **5** → L sound
- **6** → J, SH, CH, soft G sounds
- **7** → K, hard C, hard G sounds
- **8** → F, V sounds
- **9** → P, B sounds

For example: **42** → R+N sounds → **"Rain"** → Easy to visualize and remember!

## Game Logic

### Number Sequence Priority
The game follows a specific sequence order that distinguishes between single-digit and zero-prefixed numbers:

1. **Zero-prefixed numbers first**: 00, 01, 02, 03, 04, 05, 06, 07, 08, 09
2. **Then regular numbers**: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, ..., 99

This allows practitioners to have separate associations for "00" vs "0", which is important for advanced major system usage.

### Game Flow

1. **Range Selection**: Choose your practice range (e.g., "00 to 20" or "25 to 60")
2. **Random Presentation**: Numbers from your range are shown in random order
3. **Self-Paced Practice**: Take your time to mentally recall your word/image association
4. **Progress Tracking**: Each number appears exactly once per session
5. **Performance Analysis**: Get detailed statistics on your recall times

### Statistics & Performance

The game tracks:
- **Individual response times** for each number
- **Session duration** and average response time
- **Fastest and slowest** number recalls
- **Top 10 slowest numbers** to focus your practice

## Technical Architecture

Built with **C# .NET 9** using clean architecture principles:

- **`MemoApp.Core`**: Pure business logic, no UI dependencies
- **`MemoApp.Core.MajorSystem`**: Complete game engine and statistics
- **Extensible design**: Ready for MAUI mobile app, web UI, or console interfaces

### Key Classes

- **`MajorNumber`**: Represents numbers with proper zero-prefix handling
- **`GameSession`**: Manages complete training sessions
- **`NumberSequence`**: Generates correctly ordered number sequences  
- **`SessionStatistics`**: Comprehensive performance analytics
- **`GameEngine`**: Simple API for creating and running sessions

## Future Roadmap

- 🎯 **MAUI Mobile App**: Native iOS/Android interface
- 🌐 **Web Interface**: Browser-based training
- 📊 **Progress Tracking**: Long-term performance analytics
- 🎮 **Game Modes**: Timed challenges, streak tracking
- 📱 **Spaced Repetition**: Smart review of difficult numbers

## Getting Started

```bash
# Clone and build
git clone <repository-url>
cd MemoApp
dotnet build

# Run tests
dotnet test

# Run the console training app
cd MemoApp.UI.ConsoleApp
dotnet run
```

### Using the Console App

The console interface provides:
- **Quick Training**: Practice with numbers 00-09
- **Custom Range**: Choose any range like "25" to "60" or "08" to "15"
- **Major System Help**: Built-in reference for the consonant sound mappings
- **Detailed Statistics**: See your response times and identify numbers that need more practice

---

*This project explores the intersection of memory techniques, progressive learning, and modern software architecture - perfect for the Vibe Coding experimental approach to building useful, well-crafted applications.*