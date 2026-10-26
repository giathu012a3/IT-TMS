from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from PIL import Image
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

chrome_options = Options()
chrome_options.add_argument("--headless=new")
chrome_options.add_argument("--disable-gpu")
chrome_options.add_argument("--window-size=1920,1080")
chrome_options.add_argument("--force-device-scale-factor=2") # 2x DPI

try:
    driver = webdriver.Chrome(options=chrome_options)
    driver.get("data:text/html,<html><body style='background:white;'><h1 style='color:#2563eb;'>Test Selenium Chrome Headless High DPI</h1></body></html>")
    screenshot_path = r"c:\IT_System\.agents\teamwork\explorer_survey_3\test_shot.png"
    driver.save_screenshot(screenshot_path)
    driver.quit()
    
    im = Image.open(screenshot_path)
    print(f"Selenium Chrome Headless Success! Screenshot: {im.width}x{im.height}")
    if os.path.exists(screenshot_path):
        os.remove(screenshot_path)
except Exception as e:
    print(f"Selenium Chrome Error: {e}")
