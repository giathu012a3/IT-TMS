import xml.etree.ElementTree as ET
from collections import defaultdict

trx_file = r"C:\IT_System\tests\ItTms.Tests\TestResults\results.trx"
tree = ET.parse(trx_file)
root = tree.getroot()

ns = {'ns': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}

results = defaultdict(lambda: {'Passed': 0, 'Failed': 0, 'Skipped': 0, 'Total': 0})

for unit_test_result in root.findall('.//ns:UnitTestResult', ns):
    test_name = unit_test_result.attrib.get('testName', '')
    outcome = unit_test_result.attrib.get('outcome', '')
    
    # className is in UnitTest definition
    test_id = unit_test_result.attrib.get('testId', '')
    # Or find class name from testName: e.g. ItTms.Tests.UnitTests.Domain.StateMachine49PermutationTests...
    parts = test_name.split('(')[0].split('.')
    class_name = '.'.join(parts[:-1]) if len(parts) > 1 else 'Unknown'
    
    results[class_name]['Total'] += 1
    if outcome == 'Passed':
        results[class_name]['Passed'] += 1
    elif outcome == 'Failed':
        results[class_name]['Failed'] += 1
    else:
        results[class_name]['Skipped'] += 1

print(f"{'Test Class / Suite':<70} | {'Pass':<5} | {'Fail':<5} | {'Skip':<5} | {'Total':<5}")
print("=" * 95)
total_pass = total_fail = total_skip = total_all = 0
for cls, counts in sorted(results.items()):
    print(f"{cls:<70} | {counts['Passed']:<5} | {counts['Failed']:<5} | {counts['Skipped']:<5} | {counts['Total']:<5}")
    total_pass += counts['Passed']
    total_fail += counts['Failed']
    total_skip += counts['Skipped']
    total_all += counts['Total']
print("=" * 95)
print(f"{'TOTAL':<70} | {total_pass:<5} | {total_fail:<5} | {total_skip:<5} | {total_all:<5}")
