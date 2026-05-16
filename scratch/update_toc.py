import re

file_path = r'c:\Users\nt\dotnetsys\PRD-VinhKhanhPhoAmThuc.html'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# find the <div class="toc"> block
toc_start = content.find('<div class="toc">')
toc_end = content.find('</div>\n  </div>', toc_start) + 15

if toc_start == -1 or toc_end == -1:
    print("Could not find TOC")
    exit(1)

# Extract body content to find headings
body_content = content[toc_end:]

# regex to find <hX> tags
# we want to find <h2, <h3, <h4
pattern = re.compile(r'<h([2-4])[^>]*>(.*?)</h\1>', re.IGNORECASE | re.DOTALL)

toc_html = ['<div class="toc">', '  <h2>?? M?c L?c Chi Ti?t</h2>', '  <div class="toc-content" style="display: flex; flex-direction: column; gap: 4px;">']

heading_counter = 0

def clean_text(text):
    text = re.sub(r'<[^>]+>', '', text)
    return text.strip()

def generate_id(text, counter):
    import unicodedata
    # basic slugify
    text = unicodedata.normalize('NFKD', text).encode('ascii', 'ignore').decode('utf-8')
    text = re.sub(r'[^\w\s-]', '', text.lower())
    text = re.sub(r'[-\s]+', '-', text).strip('-')
    return f"toc_{counter}_{text}"

new_body = ""
last_pos = 0

for match in pattern.finditer(body_content):
    level = int(match.group(1))
    tag_content = match.group(2)
    full_tag = match.group(0)
    start_pos = match.start()
    end_pos = match.end()
    
    new_body += body_content[last_pos:start_pos]
    
    clean_title = clean_text(tag_content)
    # Check if heading already has id
    id_match = re.search(r'id=["\'](.*?)["\']', full_tag)
    if id_match:
        hid = id_match.group(1)
        new_body += full_tag
    else:
        heading_counter += 1
        hid = generate_id(clean_title, heading_counter)
        # add id to the heading tag
        new_tag = f'<h{level} id="{hid}">{tag_content}</h{level}>'
        # wait, some might have other attributes like class. Let's just do a simple replacement if no id
        # replace <hX with <hX id="..."
        new_tag = re.sub(rf'<h{level}', rf'<h{level} id="{hid}"', full_tag, count=1, flags=re.IGNORECASE)
        new_body += new_tag
        
    last_pos = end_pos
    
    # Indentation mapping
    padding = "0"
    font_size = "0.95rem"
    font_weight = "600"
    if level == 2:
        padding = "0"
        font_size = "1rem"
        font_weight = "700"
        toc_html.append(f'<div style="margin-top: 12px; border-bottom: 1px solid #eee; padding-bottom: 4px;"></div>')
    elif level == 3:
        padding = "20px"
        font_size = "0.9rem"
        font_weight = "500"
    elif level == 4:
        padding = "40px"
        font_size = "0.85rem"
        font_weight = "400"
        
    toc_html.append(f'    <a href="#{hid}" style="margin-left: {padding}; text-decoration: none; color: var(--text-secondary); font-size: {font_size}; font-weight: {font_weight}; padding: 4px 0; transition: color 0.2s;">{clean_title}</a>')

new_body += body_content[last_pos:]
toc_html.append('  </div>')
toc_html.append('</div>')

final_content = content[:toc_start] + '\n'.join(toc_html) + new_body

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(final_content)

print("TOC successfully generated and inserted.")
