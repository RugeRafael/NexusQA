import os
import re
from pathlib import Path


def read_document(file_path: str, file_content: bytes, filename: str) -> str:
    ext = Path(filename).suffix.lower()
    try:
        if ext in ['.html', '.htm']:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(file_content, 'lxml')
            return soup.get_text(separator='\n', strip=True)
        elif ext == '.md':
            return file_content.decode('utf-8', errors='ignore')
        elif ext in ['.xlsx', '.xls']:
            import openpyxl
            import io
            wb = openpyxl.load_workbook(io.BytesIO(file_content), data_only=True)
            text_parts = []
            for sheet in wb.worksheets:
                text_parts.append(f"=== Hoja: {sheet.title} ===")
                for row in sheet.iter_rows(values_only=True):
                    row_text = ' | '.join([str(c) if c is not None else '' for c in row])
                    if row_text.strip('| '):
                        text_parts.append(row_text)
            return '\n'.join(text_parts)
        elif ext == '.docx':
            from docx import Document
            import io
            doc = Document(io.BytesIO(file_content))
            return '\n'.join([p.text for p in doc.paragraphs if p.text.strip()])
        elif ext == '.txt':
            return file_content.decode('utf-8', errors='ignore')
        else:
            return file_content.decode('utf-8', errors='ignore')
    except Exception as e:
        return f"Error leyendo documento: {str(e)}"


def parse_test_plan_html(file_content: bytes) -> dict:
    """
    Parsea el HTML del plan de pruebas y extrae:
    - Metadata del proyecto
    - Tabla de RFs con nombres y rangos
    - Casos de prueba con todos sus campos
    """
    try:
        from bs4 import BeautifulSoup
        soup = BeautifulSoup(file_content, 'lxml')

        result = {
            "proyecto": "",
            "qa": "",
            "fecha": "",
            "ambiente": "",
            "total_cps": 0,
            "rfs": [],
            "casos": []
        }

        # Extraer metadata del header
        header = soup.find(class_='header')
        if header:
            meta_items = header.find_all(class_='header-meta-item')
            for item in meta_items:
                label_el = item.find(class_='label')
                value_el = item.find(class_='value')
                if label_el and value_el:
                    label = label_el.get_text(strip=True).lower()
                    value = value_el.get_text(strip=True)
                    if 'proyecto' in label:
                        result['proyecto'] = value
                    elif 'qa' in label or 'engineer' in label:
                        result['qa'] = value
                    elif 'fecha' in label:
                        result['fecha'] = value
                    elif 'ambiente' in label:
                        result['ambiente'] = value

        # Extraer tabla de RFs
        sections = soup.find_all(class_='section')
        for section in sections:
            h2 = section.find('h2')
            if not h2:
                continue
            title = h2.get_text(strip=True)

            # Tabla de Requerimientos Funcionales
            if 'Requerimientos' in title or 'Requisitos' in title:
                rows = section.find_all('tr')
                for row in rows:
                    cells = row.find_all('td')
                    if len(cells) >= 3:
                        rf_id = cells[0].get_text(strip=True)
                        rf_nombre = cells[1].get_text(strip=True)
                        rf_cantidad = cells[2].get_text(strip=True)
                        rf_rango = cells[3].get_text(strip=True) if len(cells) > 3 else ''
                        if rf_id.startswith('RF-') or rf_id.startswith('RF '):
                            result['rfs'].append({
                                'id': rf_id,
                                'nombre': rf_nombre,
                                'cantidad': rf_cantidad,
                                'rango': rf_rango
                            })

            # Tabla de Casos de Prueba
            elif 'Casos de Prueba' in title or 'Casos' in title:
                tc_table = section.find(class_='tc-table')
                if not tc_table:
                    tc_table = section.find('table')
                if not tc_table:
                    continue

                current_rf = ''
                current_rf_nombre = ''
                rows = tc_table.find_all('tr')

                for row in rows:
                    # Fila de bloque RF
                    if 'block-header' in row.get('class', []):
                        header_text = row.get_text(strip=True)
                        rf_match = re.search(r'(RF-[\d,\s]+)', header_text)
                        if rf_match:
                            current_rf = rf_match.group(1).strip()
                        # Extraer nombre despues del guion
                        parts = header_text.split('—')
                        if len(parts) > 1:
                            current_rf_nombre = parts[1].strip()
                        elif len(parts) == 1:
                            current_rf_nombre = header_text.replace(current_rf, '').strip(' —')
                        continue

                    # Fila de caso de prueba
                    cells = row.find_all('td')
                    if len(cells) < 3:
                        continue

                    cp_id_el = row.find(class_='tc-id')
                    cp_name_el = row.find(class_='tc-name')
                    cp_rf_el = row.find(class_='tc-rf')

                    if not cp_id_el:
                        continue

                    cp_id = cp_id_el.get_text(strip=True)
                    if not cp_id.startswith('CP-'):
                        continue

                    cp_name = cp_name_el.get_text(strip=True) if cp_name_el else ''
                    cp_rf = cp_rf_el.get_text(strip=True) if cp_rf_el else current_rf

                    # Modulo y submodulo
                    modulo = cells[3].get_text(strip=True) if len(cells) > 3 else ''
                    submodulo = cells[4].get_text(strip=True) if len(cells) > 4 else ''

                    # Criticidad
                    crit_badge = row.find(class_='crit-badge')
                    criticidad = crit_badge.get_text(strip=True) if crit_badge else 'Media'

                    # Marcado especial
                    is_marked = 'marked' in row.get('class', [])

                    result['casos'].append({
                        'id': cp_id,
                        'nombre': cp_name,
                        'rf': cp_rf,
                        'rf_nombre': current_rf_nombre,
                        'modulo': modulo,
                        'submodulo': submodulo,
                        'criticidad': criticidad,
                        'marcado': is_marked
                    })

        result['total_cps'] = len(result['casos'])
        return result

    except Exception as e:
        return {
            "proyecto": "", "qa": "", "fecha": "", "ambiente": "",
            "total_cps": 0, "rfs": [], "casos": [],
            "error": str(e)
        }
