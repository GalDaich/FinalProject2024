# 🌍 Welcome to TripMatch! 

Tripmatch is a  project designed to help travelers find their perfect travel buddies!
TripMatch is a web application that helps users find compatible travel companions based on their travel preferences and interests using machine learning clustering algorithms.

### 🎯 Main Features
- ✅ Create your traveler profile
- 📅 Set your travel timeline
- 🤝 Find travelers with similar plans
- ⭐ Select your favorite travel buddies
- 📱 Connect with potential travel companions


## Technologies Used for Tripmatch:

### Backend
- ASP.NET Core
- MongoDB for database
- Python Flask API

### Frontend
- Razor Pages
- Bootstrap 5
- jQuery

### Machine Learning
- **Python**: Primary language for our clustering implementation
- **K-modes Clustering**: Chosen specifically for categorical data handling
- **Pandas for Data Processing**


## 🚀 Installation Guide

### Prerequisites
Before you start, make sure you have the following installed:
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Python 3.8+](https://www.python.org/downloads/)
- [MongoDB Community Server](https://www.mongodb.com/try/download/community)

## Step-by-Step Installation - 
### Setting up the Python Environment:
#### Step 1: Open Command Prompt
#### Step 2: Navigate to User Directory
➜ Run this in cmd: cd %USERPROFILE%
#### Step 3: Create virtual environment
➜ Run this in cmd: - python -m venv venv
#### Step 4: Activate virtual environment
➜ Run this in cmd: venv\Scripts\activate
#### Step 5: Update setuptools and pip (IMPORTANT - do these in order!) -
➜ Run this in cmd:
python -m pip install setuptools<br>
python -m pip install --upgrade pip<br>
#### Step 6: Install necessary packages
➜ Run this in cmd: - pip install numpy pandas scikit-learn flask pymongo kmodes
#### Step 7: Navigate to machine learning folder
➜ Run this in cmd: - cd \FinalProject2024\ML-Clustering

### Run clustering algorithm on Travelplans DB:
After navigating to the machine learning folder ➜ Run this in cmd: python ClusterDB.py<br>
Wait for process to complete

### Setting up flask application
Open a new CMD<br>
Start up the virual environment<br> 
Navigate to the machine learning folder and start the flask app ➜ Run this in cmd: python app.py

## Open MongoDB compass

## Run Tripmatch project solution