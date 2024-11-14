import pandas as pd
from pymongo import MongoClient
from datetime import datetime
import logging
from typing import Optional
import re
import sys

# Set up logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('user_upload.log'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)

class UserDataUploader:
    def __init__(self, connection_string: str, database_name: str):
        """Initialize MongoDB connection and database."""
        try:
            self.client = MongoClient(connection_string)
            self.db = self.client[database_name]
            self.users_collection = self.db['Users']
            logger.info("Successfully connected to MongoDB")
        except Exception as e:
            logger.error(f"Failed to connect to MongoDB: {str(e)}")
            raise

    def validate_email(self, email: str) -> bool:
        """Validate email format."""
        email_pattern = re.compile(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$')
        return bool(email_pattern.match(email))

    def format_phone_number(self, phone: str) -> str:
        """
        Format phone number while preserving leading zeros.
        Returns the formatted phone number as a string.
        """
        try:
            # Convert to string if it's not already
            phone_str = str(phone)
            
            # Remove any non-digit characters except leading plus
            if phone_str.startswith('+'):
                cleaned = '+' + ''.join(filter(str.isdigit, phone_str))
            else:
                cleaned = ''.join(filter(str.isdigit, phone_str))
            
            # If it's just digits (no plus), ensure leading zero is preserved
            if not cleaned.startswith('+'):
                # Pad with leading zero if it's supposed to start with zero but was dropped
                if phone_str.startswith('0') and not cleaned.startswith('0'):
                    cleaned = '0' + cleaned

            return cleaned
        except Exception as e:
            logger.error(f"Error formatting phone number {phone}: {str(e)}")
            return str(phone)  # Return original if formatting fails

    def validate_phone(self, phone: str) -> bool:
        """
        Validate phone number format.
        Allows for leading zeros and various international formats.
        """
        # First, format the phone number
        formatted_phone = self.format_phone_number(phone)
        
        # Allow for:
        # - Numbers starting with + (international)
        # - Numbers starting with 0 (local)
        # - 9-15 digits (with or without +)
        phone_pattern = re.compile(r'^(?:\+\d{9,15}|\d{9,15}|0\d{8,14})$')
        return bool(phone_pattern.match(formatted_phone))

    def calculate_age(self, birth_date: datetime) -> int:
        """Calculate current age based on birth date."""
        today = datetime.today()
        age = today.year - birth_date.year
        if birth_date.date() > today.date().replace(year=birth_date.year):
            age -= 1
        return age

    def normalize_data(self, data: dict) -> dict:
        """Normalize user data according to the application's requirements."""
        return {
            'fullName': str(data['fullName']).strip(),
            'emailAddress': str(data['emailAddress']).lower().strip(),
            'password': str(data['password']).strip(),
            'phoneNumber': self.format_phone_number(data['phoneNumber']),
            'dateOfBirth': data['dateOfBirth'],
            'currentAge': self.calculate_age(data['dateOfBirth']),
            'livesAt': str(data['livesAt']).strip(),
            'hobby1': str(data['hobby1']).strip(),
            'hobby2': str(data['hobby2']).strip()
        }

    def validate_user_data(self, data: dict) -> tuple[bool, Optional[str]]:
        """Validate user data fields."""
        if not all(data.values()):
            return False, "All fields are required"
        
        if not self.validate_email(data['emailAddress']):
            return False, f"Invalid email format: {data['emailAddress']}"
        
        if not self.validate_phone(data['phoneNumber']):
            return False, f"Invalid phone number format: {data['phoneNumber']}"
        
        if len(data['password']) < 6:
            return False, "Password must be at least 6 characters long"
        
        return True, None

    def upload_users(self, csv_path: str) -> tuple[int, int]:
        """
        Upload users from CSV file to MongoDB.
        Returns tuple of (success_count, error_count)
        """
        success_count = 0
        error_count = 0

        try:
            # Read CSV file with phone numbers as strings
            df = pd.read_csv(csv_path, dtype={'phoneNumber': str})
            df['dateOfBirth'] = pd.to_datetime(df['dateOfBirth'])
            total_records = len(df)
            logger.info(f"Starting upload of {total_records} records")

            # Log the first few records for verification
            logger.info("Sample of first few records:")
            for _, row in df.head().iterrows():
                logger.info(f"Phone number from CSV: {row['phoneNumber']}")

            for index, row in df.iterrows():
                try:
                    # Convert row to dict and normalize data
                    user_data = row.to_dict()
                    normalized_data = self.normalize_data(user_data)

                    # Log phone number transformations for debugging
                    logger.info(f"Row {index + 1}:")
                    logger.info(f"Original phone: {row['phoneNumber']}")
                    logger.info(f"Normalized phone: {normalized_data['phoneNumber']}")

                    # Validate data
                    is_valid, error_message = self.validate_user_data(normalized_data)
                    if not is_valid:
                        logger.error(f"Row {index + 1}: {error_message}")
                        error_count += 1
                        continue

                    # Check for existing user with same email or phone
                    existing_user = self.users_collection.find_one({
                        '$or': [
                            {'emailAddress': normalized_data['emailAddress']},
                            {'phoneNumber': normalized_data['phoneNumber']}
                        ]
                    })

                    if existing_user:
                        logger.warning(f"Row {index + 1}: User already exists with email {normalized_data['emailAddress']} or phone {normalized_data['phoneNumber']}")
                        error_count += 1
                        continue

                    # Insert user
                    self.users_collection.insert_one(normalized_data)
                    success_count += 1
                    logger.info(f"Successfully uploaded user {normalized_data['emailAddress']}")

                except Exception as e:
                    logger.error(f"Error processing row {index + 1}: {str(e)}")
                    error_count += 1

            logger.info(f"Upload complete. Successful: {success_count}, Failed: {error_count}")
            return success_count, error_count

        except Exception as e:
            logger.error(f"Failed to read or process CSV file: {str(e)}")
            raise

    def close(self):
        """Close MongoDB connection."""
        self.client.close()
        logger.info("MongoDB connection closed")

def main():
    # Connection settings
    CONNECTION_STRING = "mongodb://localhost:27017"  # Update with your MongoDB connection string
    DATABASE_NAME = "TripMatch"  # Update with your database name
    CSV_PATH = "users.csv"  # Update with your CSV file path

    try:
        uploader = UserDataUploader(CONNECTION_STRING, DATABASE_NAME)
        success_count, error_count = uploader.upload_users(CSV_PATH)
        
        logger.info(f"""
        Upload Summary:
        ---------------
        Total Successful: {success_count}
        Total Failed: {error_count}
        Total Processed: {success_count + error_count}
        """)
    
    except Exception as e:
        logger.error(f"Upload failed: {str(e)}")
        sys.exit(1)
    finally:
        uploader.close()

if __name__ == "__main__":
    main()