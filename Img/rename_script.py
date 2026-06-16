import os
import re

def rename_images(folder_path):
    # Ensure the folder path exists
    if not os.path.exists(folder_path):
        print(f"Error: The folder '{folder_path}' does not exist.")
        return

    # list all files in the directory
    files = os.listdir(folder_path)
    
    # Filter for files that match the "imagen(X)" pattern
    target_files = []
    for f in files:
        match = re.match(r'imagen\((\d+)\)', f, re.IGNORECASE)
        if match:
            # Extract the number so we can sort them correctly (22, 23, 24...)
            file_num = int(match.group(1))
            if 22 <= file_num <= 66:
                target_files.append((file_num, f))
    
    # Sort files by their original number so 22 becomes 01, 23 becomes 02, etc.
    target_files.sort()
    
    if not target_files:
        print("No matching 'imagen(22)' to 'imagen(66)' files found.")
        return

    # FIX: Added proper f-string formatting here
    print(f"Found {len(target_files)} files to rename...")

    # Rename the files sequentially
    for index, (file_num, filename) in enumerate(target_files, start=1):
        # Extract the file extension (e.g., .jpg, .png)
        ext = os.path.splitext(filename)[1]
        
        # Format the new name with a 2-digit zero-padded number (01, 02, etc.)
        new_name = f"Path-Tracer-UNITY-{index:02d}{ext}"
        
        # Get full absolute paths
        old_file_path = os.path.join(folder_path, filename)
        new_file_path = os.path.join(folder_path, new_name)
        
        # Rename the file
        os.rename(old_file_path, new_file_path)
        print(f"Renamed: {filename} -> {new_name}")

    print("Success! All files have been renamed.")

# Based on your error message, I updated this path for you automatically:
TARGET_FOLDER = r"C:\Code\MathCode\Path-Tracer-UNITY\Img"

if __name__ == "__main__":
    rename_images(TARGET_FOLDER)