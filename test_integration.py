import sys
import os
import time
import asyncio
import pandas as pd

# 添加当前目录到 sys.path
sys.path.append(os.getcwd())

def print_header(msg):
    print("\n" + "="*60)
    print(f"TEST: {msg}")
    print("="*60)

async def test_async_crawler():
    print_header("Testing Async Crawler (quant_system)")
    try:
        from quant_system.crawler.async_crawler import AsyncNewsCrawler
        crawler = AsyncNewsCrawler()
        
        symbol = "600519"
        print(f"Fetching news for {symbol}...")
        start = time.time()
        news = await crawler.search_news(symbol, days=7)
        end = time.time()
        
        print(f"✅ Successfully fetched {len(news)} news items in {end-start:.2f}s")
        if news:
            print(f"Sample: {news[0]['title']}")
    except Exception as e:
        print(f"❌ Async Crawler test failed: {e}")
        import traceback
        traceback.print_exc()

def test_data_source():
    print_header("Testing Data Source (EastMoneyData)")
    try:
        from data_source import EastMoneyData
        em = EastMoneyData()
        
        print("Fetching realtime quotes...")
        df = em.get_realtime_quotes(count=10)
        if not df.empty:
            print(f"✅ Successfully fetched {len(df)} quotes")
            print(df[['代码', '名称', '最新价', '涨跌幅']].head(3))
        else:
            print("⚠️ Fetched empty dataframe (might be market closed or network issue)")
            
    except Exception as e:
        print(f"❌ Data Source test failed: {e}")
        import traceback
        traceback.print_exc()

def test_selector():
    print_header("Testing Selector (ComprehensiveSelector)")
    try:
        from selector import ComprehensiveSelector
        selector = ComprehensiveSelector()
        
        print("Testing get_buy_signals (this might take a while)...")
        # 为了测试速度，我们可以mock一些数据或者只运行简单的逻辑
        # 这里我们尝试运行一下，如果太慢就手动中断
        
        # 由于get_buy_signals会获取实时行情，我们先简单测试一下技术分析函数
        symbol = "000001"
        print(f"Analyzing technical indicators for {symbol}...")
        analysis = selector.analyze_stock_technical(symbol)
        print(f"✅ Analysis result: {analysis}")
        
    except Exception as e:
        print(f"❌ Selector test failed: {e}")
        import traceback
        traceback.print_exc()

def main():
    print("Starting Integration Test...")
    
    # Run async tests
    asyncio.run(test_async_crawler())
    
    # Run synchronous tests
    test_data_source()
    test_selector()
    
    print("\n" + "="*60)
    print("ALL TESTS COMPLETED")
    print("="*60)
    print("\nTo start the web application, run:")
    print("streamlit run app.py")

if __name__ == "__main__":
    main()
