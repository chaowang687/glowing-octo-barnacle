import os
import time
import json
import logging
import random
import pandas as pd
from typing import Optional, List, Dict, Union

# Set up logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

class BrowserDataSource:
    def __init__(self, headless: bool = True):
        self.headless = headless
        self.driver_type = self._detect_driver()
        logger.info(f"Using browser automation driver: {self.driver_type}")
        
    def _detect_driver(self):
        """
        Detect installed browser automation libraries.
        Prioritizes Playwright over Selenium.
        """
        try:
            import playwright
            return 'playwright'
        except ImportError:
            pass
        
        try:
            import selenium
            return 'selenium'
        except ImportError:
            pass
            
        raise ImportError(
            "Neither 'playwright' nor 'selenium' is installed. "
            "Please install one of them (e.g., `pip install playwright` or `pip install selenium`)."
        )

    def _get_user_agent(self):
        user_agents = [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.114 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.101 Safari/537.36"
        ]
        return random.choice(user_agents)

    def get_realtime_quotes(self, count: int = 100) -> pd.DataFrame:
        """
        Get realtime quotes for A-shares using EastMoney API via browser.
        Fetches top `count` stocks.
        """
        # EastMoney API for A-shares list
        # We use a browser to fetch the JSON API response directly to bypass anti-bot
        url = (
            f"http://push2.eastmoney.com/api/qt/clist/get?pn=1&pz={count}&po=1&np=1"
            "&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&invt=2&fid=f3"
            "&fs=m:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048"
            "&fields=f12,f14,f2,f3,f4,f5,f6,f7,f15,f16,f17,f18"
        )
        # Fields mapping: f12=code, f14=name, f2=price, f3=percent, f4=change, f5=volume, f6=amount...
        
        try:
            data = self._fetch_url(url)
            if not data or 'data' not in data or 'diff' not in data['data']:
                logger.error("Invalid data format received from EastMoney")
                return pd.DataFrame()
                
            items = data['data']['diff']
            df = pd.DataFrame(items)
            
            # Rename columns
            rename_map = {
                'f12': 'symbol',
                'f14': 'name',
                'f2': 'price',
                'f3': 'percent',
                'f4': 'change',
                'f5': 'volume',
                'f6': 'amount',
                'f15': 'high',
                'f16': 'low',
                'f17': 'open',
                'f18': 'prev_close'
            }
            # Only rename existing columns
            df = df.rename(columns=rename_map)
            
            # Ensure price fields are numeric
            numeric_cols = ['price', 'percent', 'change', 'volume', 'amount', 'high', 'low', 'open', 'prev_close']
            for col in numeric_cols:
                if col in df.columns:
                    df[col] = pd.to_numeric(df[col], errors='coerce')

            return df
            
        except Exception as e:
            logger.error(f"Failed to get realtime quotes: {e}")
            return pd.DataFrame()

    def get_stock_kline(self, symbol: str, start_date: str = '20200101', period: str = '101') -> pd.DataFrame:
        """
        Get K-line data for a specific stock.
        symbol: Stock code (e.g., '600519')
        start_date: 'YYYYMMDD'
        period: '101' for daily, '102' for weekly, etc.
        """
        # Determine market (secid)
        # 0: Shenzhen (0xxxx, 3xxxx), 1: Shanghai (6xxxx)
        market = '1' if symbol.startswith(('6', '9')) else '0'
        secid = f"{market}.{symbol}"
        
        # URL for K-line
        url = (
            f"http://push2his.eastmoney.com/api/qt/stock/kline/get?secid={secid}"
            f"&fields1=f1,f2,f3,f4,f5,f6&fields2=f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61"
            f"&klt={period}&fqt=1&beg={start_date}&end=20500101"
        )
        
        try:
            data = self._fetch_url(url)
            if not data or 'data' not in data or 'klines' not in data['data']:
                logger.warning(f"No K-line data found for {symbol}. Response might be empty.")
                return pd.DataFrame()
                
            klines = data['data']['klines']
            # Format: "2023-01-01,open,close,high,low,vol,amt,..."
            
            records = []
            for k in klines:
                parts = k.split(',')
                if len(parts) >= 7:
                    records.append({
                        'date': parts[0],
                        'open': float(parts[1]),
                        'close': float(parts[2]),
                        'high': float(parts[3]),
                        'low': float(parts[4]),
                        'volume': float(parts[5]),
                        'amount': float(parts[6])
                    })
            
            df = pd.DataFrame(records)
            if not df.empty:
                df['date'] = pd.to_datetime(df['date'])
                
            return df
            
        except Exception as e:
            logger.error(f"Failed to get K-line for {symbol}: {e}")
            return pd.DataFrame()

    def _fetch_url(self, url: str, retries: int = 3) -> dict:
        for attempt in range(retries):
            try:
                if self.driver_type == 'playwright':
                    return self._fetch_playwright(url)
                else:
                    return self._fetch_selenium(url)
            except Exception as e:
                logger.warning(f"Attempt {attempt + 1}/{retries} failed: {e}")
                time.sleep(random.uniform(1, 3))
        
        logger.error(f"All retries failed for fetching URL: {url}")
        return {}

    def _fetch_playwright(self, url: str) -> dict:
        from playwright.sync_api import sync_playwright
        
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=self.headless)
            try:
                context = browser.new_context(user_agent=self._get_user_agent())
                page = context.new_page()
                
                # Add random delay to simulate human behavior
                time.sleep(random.uniform(0.5, 1.5))
                
                response = page.goto(url, wait_until='domcontentloaded')
                if not response.ok:
                    raise Exception(f"HTTP status {response.status}")
                
                content = page.inner_text("body")
                return json.loads(content)
            finally:
                browser.close()

    def _fetch_selenium(self, url: str) -> dict:
        from selenium import webdriver
        from selenium.webdriver.chrome.options import Options
        from selenium.webdriver.common.by import By
        
        options = Options()
        if self.headless:
            options.add_argument("--headless")
        
        options.add_argument(f"user-agent={self._get_user_agent()}")
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
        # Ensure system proxy is used (default behavior)
        
        driver = webdriver.Chrome(options=options)
        try:
            driver.get(url)
            time.sleep(random.uniform(0.5, 1.5))
            
            # EastMoney API usually returns JSON in the body
            content = driver.find_element(By.TAG_NAME, "body").text
            return json.loads(content)
        finally:
            driver.quit()

if __name__ == "__main__":
    # Example usage
    try:
        browser_source = BrowserDataSource(headless=True)
        
        # Test 1: Realtime Quotes
        print("Fetching Realtime Quotes...")
        df_quotes = browser_source.get_realtime_quotes(count=5)
        print(df_quotes.head())
        
        # Test 2: K-Line
        print("\nFetching K-Line for 600519 (Moutai)...")
        df_kline = browser_source.get_stock_kline('600519', start_date='20230101')
        print(df_kline.head())
        
    except ImportError as e:
        print(e)
    except Exception as e:
        print(f"An error occurred: {e}")
