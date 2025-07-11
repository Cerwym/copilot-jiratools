# Creating a Homebrew Tap for JiraTools

To make JiraTools available through Homebrew, you need to create a Homebrew tap repository.

## 1. Create the tap repository

Create a new GitHub repository named `homebrew-tap` (this naming convention is required by Homebrew).

The full repository name should be: `peterlockett/homebrew-tap`

## 2. Initialize the repository

```bash
# Clone the new repository
git clone https://github.com/peterlockett/homebrew-tap.git
cd homebrew-tap

# Create the Formula directory
mkdir Formula

# Copy the formula file
cp /path/to/copilot-jiratools/Formula/jiratools.rb Formula/

# Create README
cat > README.md << 'EOF'
# Homebrew Tap for JiraTools

This is a custom Homebrew tap that provides the JiraTools CLI.

## Installation

```bash
brew tap peterlockett/tap
brew install jiratools
```

## Available Formulas

- **jiratools**: A command-line tool for interacting with Jira from development environments

## Links

- [JiraTools Source Code](https://github.com/peterlockett/copilot-jiratools)
EOF

# Commit and push
git add .
git commit -m "Initial tap setup with jiratools formula"
git push origin main
```

## 3. Test the tap locally

```bash
# Add the tap
brew tap peterlockett/tap

# Install from your tap
brew install peterlockett/tap/jiratools

# Test the installation
jiratools --help
```

## 4. Updating the formula

After each release of JiraTools:

1. Run the update script: `./scripts/update-homebrew-formula.sh 1.0.1`
2. Copy the updated formula to your tap repository
3. Commit and push the changes

```bash
# In your homebrew-tap repository
cp /path/to/copilot-jiratools/Formula/jiratools.rb Formula/
git add Formula/jiratools.rb
git commit -m "Update jiratools to v1.0.1"
git push origin main
```

## 5. Users can then install/update

```bash
# Install
brew install peterlockett/tap/jiratools

# Update
brew update
brew upgrade jiratools
```
