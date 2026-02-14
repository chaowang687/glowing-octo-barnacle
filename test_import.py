import sys
import os

# 确保当前目录在 sys.path 中
sys.path.append(os.getcwd())

print("Testing imports...")

try:
    from quant_system.crawler.crawler import NewsCrawler
    print("✅ Successfully imported NewsCrawler")
except ImportError as e:
    print(f"❌ Failed to import NewsCrawler: {e}")

try:
    from quant_system.config import DB_PATH
    print(f"✅ Successfully imported config. DB_PATH: {DB_PATH}")
except ImportError as e:
    print(f"❌ Failed to import config: {e}")

try:
    from quant_system.analysis.market_analyzer import MarketAnalyzer
    print("✅ Successfully imported MarketAnalyzer")
    analyzer = MarketAnalyzer()
    print("✅ Successfully initialized MarketAnalyzer")
except ImportError as e:
    print(f"❌ Failed to import MarketAnalyzer: {e}")
except Exception as e:
    print(f"❌ Failed to initialize MarketAnalyzer: {e}")
