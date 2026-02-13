# A股全能量化选股系统

## 项目说明
- 本系统融合了缠论结构分析、CPV成交量动力学、基本面筛选
- 目标：帮助百万级本金的个人交易者系统化选股

## 环境依赖
```
pip install akshare pandas mplfinance plotly streamlit sqlalchemy numpy
```

## 运行方式
```bash
# 启动网页界面
streamlit run app.py

# 或者运行选股脚本
python main.py
```

## 项目结构
- `app.py` - Streamlit网页界面
- `main.py` - 主程序入口
- `chanlun_engine.py` - 缠论量化引擎（K线包含、分型、笔、中枢、买卖点）
- `data_source.py` - 数据获取模块（东方财富API）
- `data/` - 数据存储目录
- `utils/` - 工具函数

## 数据源
- **东方财富API** (推荐): 无需Token，直接可用的实时行情、K线数据
- **Tushare Pro**: 需要Token，部分接口需要积分

## 使用示例
```python
from data_source import get_quotes, get_kline, get_index
from chanlun_engine import analyze_stock

# 获取实时行情
df = get_quotes(100)

# 获取K线
kline = get_kline('000001')

# 获取指数
index_df = get_index()

# 缠论分析
result = analyze_stock(kline)
```
```python
from chanlun_engine import ChanQuantEngine, analyze_stock

# 方式1：使用引擎类
engine = ChanQuantEngine(bi_threshold=0.03, use_macd=True)
signals = engine.run(df)

# 方式2：使用便捷函数
result = analyze_stock(df, bi_threshold=0.03)
print(result['summary'])  # 摘要
print(result['bi'])       # 笔列表
print(result['zhongshu']) # 中枢列表
```
