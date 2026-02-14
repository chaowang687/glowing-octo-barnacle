import os

def dedent_lines(lines):
    if not lines: return []
    # 找到第一行非空行的缩进
    indent = 0
    for line in lines:
        if line.strip():
            indent = len(line) - len(line.lstrip())
            break
    
    if indent == 0: return lines
    
    return [line[indent:] if len(line) >= indent else line.lstrip() for line in lines]

with open('app.py', 'r') as f:
    lines = f.readlines()

# Header: 1 to 476 (index 0 to 476)
header = lines[:476]

# Market Overview: 484 to 1317 (index 483 to 1317)
# 483 is the if statement, we want the body which starts at 484
market_lines = lines[484:1318] 
market_lines = dedent_lines(market_lines)
with open('pages/1_Market_Overview.py', 'w') as f:
    f.writelines(header + market_lines)

# Stock Selection: 1319 to 1497 (index 1318 to 1497)
select_lines = lines[1319:1498]
select_lines = dedent_lines(select_lines)
with open('pages/2_Stock_Selection.py', 'w') as f:
    f.writelines(header + select_lines)

# Backtest: 1499 to End (index 1498 to End)
backtest_lines = lines[1499:]
backtest_lines = dedent_lines(backtest_lines)
with open('pages/3_Backtest.py', 'w') as f:
    f.writelines(header + backtest_lines)

print("Split complete.")
