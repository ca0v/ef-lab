# Setup Instructions

## Prerequisites

### 1. Install .NET SDK 8.0

**Option A: Using the install script (Recommended for Linux)**
```bash
npm run install:dotnet:script
# Then add to your ~/.bashrc or ~/.bash_profile:
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
# Reload your shell:
source ~/.bashrc
```

**Option B: Package Manager (Ubuntu/Debian)**
```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

**Option C: Manual Download**
Visit https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Verify Installation
```bash
dotnet --version
# Should output: 8.0.x
```

### 3. Add to Shell Profile (Permanent)
Add these lines to your `~/.bashrc` (or `~/.bash_profile`):
```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
```

Then reload:
```bash
source ~/.bashrc
```

**Alternative**: Use the helper script `./npm-dotnet.sh` to run npm commands without permanently modifying your PATH.

## Project Setup

Once .NET is installed, run:
```bash
npm run setup
```

This will:
1. Create the EFLab class library project
2. Install Entity Framework Core packages (InMemory and SQLite)
3. Install the dotnet-ef global tool

## Available Scripts

### Setup & Installation
- `npm run install:dotnet:help` - Show .NET installation instructions
- `npm run install:dotnet:script` - Download and run .NET installer
- `npm run install:ef` - Install/update dotnet-ef tool
- `npm run setup` - Complete project setup (after dotnet is installed)

### Development
- `npm run build` - Build the project
- `npm test` - Run all tests
- `npm run test:pattern -- "pattern"` - Run tests matching pattern
- `npm run watch` - Run tests in watch mode
- `npm run clean` - Clean build artifacts

### Entity Framework Tools
- `npm run ef:list` - Show EF commands
- `npm run ef:migrations:add -- MigrationName` - Add new migration
- `npm run ef:migrations:list` - List all migrations
- `npm run ef:database:update` - Update database to latest migration
- `npm run ef:database:drop` - Drop the database

## Quick Start (After Setup)

```bash
# Run all tests
npm test

# Run specific test pattern
npm run test:pattern -- "DbContext"

# Watch mode for development
npm run watch
```
