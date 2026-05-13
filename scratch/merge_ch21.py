import sys

filename = 'VinhKhanh-PRD (1).html'
with open(filename, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 1. Update Top Nav TOC
nav_insert_idx = -1
for i, line in enumerate(lines):
    if '<li><a href="#ch20">Admin</a></li>' in line:
        nav_insert_idx = i + 1
        break

if nav_insert_idx != -1:
    lines.insert(nav_insert_idx, '      <li><a href="#ch21">Kiểm Thử</a></li>\n')

# 2. Update Main TOC
toc_insert_idx = -1
for i, line in enumerate(lines):
    if '<a href="#ch20">20 · Quản Trị Admin (Web)</a>' in line:
        toc_insert_idx = i + 1
        break

if toc_insert_idx != -1:
    lines.insert(toc_insert_idx, '      <a href="#ch21">21 · Hướng Dẫn Kiểm Thử & Deploy</a>\n')

# 3. Insert new section at the end, right before <script> (which usually follows the last </div>)
script_idx = -1
for i, line in enumerate(lines):
    if '<script>' in line and 'mermaid.initialize' in lines[i+1]:
        script_idx = i
        break

if script_idx != -1:
    with open('scratch/ch21.html', 'r', encoding='utf-8') as new_f:
        ch21_content = new_f.readlines()
    
    # insert ch21_content at script_idx
    lines = lines[:script_idx] + ch21_content + ['\n'] + lines[script_idx:]

with open(filename, 'w', encoding='utf-8') as f:
    f.writelines(lines)

print("Merged Test-guide.md as Chapter 21 into the PRD successfully.")
