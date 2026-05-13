import sys

filename = 'VinhKhanh-PRD (1).html'
with open(filename, 'r', encoding='utf-8') as f:
    lines = f.readlines()

nav_insert_idx = -1
for i, line in enumerate(lines):
    if '<li><a href="#ch18">NFR</a></li>' in line:
        nav_insert_idx = i + 1
        break

if nav_insert_idx != -1:
    lines.insert(nav_insert_idx, '      <li><a href="#ch19">Tổng Hợp</a></li>\n      <li><a href="#ch20">Admin</a></li>\n')

toc_insert_idx = -1
for i, line in enumerate(lines):
    if '<a href="#ch19">19 · Tổng Hợp & Chứng Minh</a>' in line:
        toc_insert_idx = i + 1
        break

if toc_insert_idx != -1:
    lines.insert(toc_insert_idx, '      <a href="#ch20">20 · Quản Trị Admin (Web)</a>\n')

start_replace_idx = -1
end_replace_idx = -1

for i, line in enumerate(lines):
    if '<!-- ===== CH19 ADDITIONAL SEQUENCE DIAGRAMS ===== -->' in line:
        start_replace_idx = i
        break

if start_replace_idx != -1:
    for i in range(start_replace_idx, len(lines)):
        if '<script>' in lines[i]:
            end_replace_idx = i - 1
            break

if start_replace_idx != -1 and end_replace_idx != -1:
    with open('scratch/new_admin_sequences.html', 'r', encoding='utf-8') as new_f:
        new_content = new_f.readlines()
    
    # We want to keep the final `  </div>` or just replace everything and make sure `new_admin_sequences.html` has the closing tag.
    # Wait, in the original HTML:
    # 7820: 
    # 7821:   <!-- ===== CH19 ADDITIONAL ...
    # 8172:   </div>
    # 8173: 
    # 8174:     <script>
    # So replacing 7821 to 8173 with new_admin_sequences + "\n"
    # But does new_admin_sequences.html close all its divs?
    # Let's count divs in new_admin_sequences.html:
    # <div class="sec" id="ch20"> starts
    # inside we have <div class="sh">, <div class="card">, etc. which are closed.
    # We NEED to add `  </div>\n` at the end of new_content!
    new_content.append('  </div>\n')

    lines[start_replace_idx:end_replace_idx+1] = new_content

with open(filename, 'w', encoding='utf-8') as f:
    f.writelines(lines)

print(f"Successfully updated {filename}")
print(f"Replaced from {start_replace_idx} to {end_replace_idx}")
