import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const resultsDir = path.resolve(process.cwd(), 'test-results');
const resultsJsonPath = path.join(resultsDir, 'results.json');
const summaryPath = path.join(resultsDir, 'summary.md');

function flattenSpecs(suites, bucket = []) {
  for (const suite of suites ?? []) {
    flattenSpecs(suite.suites, bucket);

    for (const spec of suite.specs ?? []) {
      bucket.push(spec);
    }
  }

  return bucket;
}

function getLatestResult(test) {
  const results = test?.results ?? [];
  return results.at(-1) ?? null;
}

function getSpecStatus(spec) {
  const latestStatuses = (spec.tests ?? [])
    .map(getLatestResult)
    .map((result) => result?.status)
    .filter(Boolean);

  if (latestStatuses.length === 0) {
    return 'unknown';
  }

  if (latestStatuses.every((status) => status === 'skipped')) {
    return 'skipped';
  }

  if (latestStatuses.some((status) => ['failed', 'timedOut', 'interrupted'].includes(status))) {
    return latestStatuses.some((status) => status === 'passed') ? 'flaky' : 'failed';
  }

  if (latestStatuses.every((status) => status === 'passed')) {
    return 'passed';
  }

  return 'unknown';
}

function getSpecDuration(spec) {
  return (spec.tests ?? [])
    .map(getLatestResult)
    .reduce((total, result) => total + (result?.duration ?? 0), 0);
}

function getSpecErrorText(spec) {
  for (const test of spec.tests ?? []) {
    const latestResult = getLatestResult(test);
    const message = latestResult?.error?.message ?? latestResult?.errors?.[0]?.message;

    if (message) {
      return message.split('\n').map((line) => line.trim()).filter(Boolean)[0] ?? 'Unknown error';
    }
  }

  return '';
}

function formatDuration(durationMs) {
  const seconds = (durationMs / 1000).toFixed(2);
  return `${seconds}s`;
}

function escapeTableCell(value) {
  return String(value ?? '').replace(/\|/g, '\\|').replace(/\n/g, ' ');
}

export async function generateSummary() {
  const raw = await readFile(resultsJsonPath, 'utf8');
  const report = JSON.parse(raw);
  const specs = flattenSpecs(report.suites ?? []);
  const summarizedSpecs = specs.map((spec) => ({
    title: spec.title ?? 'Untitled spec',
    file: spec.file ?? 'unknown',
    line: spec.line ?? '-',
    status: getSpecStatus(spec),
    duration: getSpecDuration(spec),
    error: getSpecErrorText(spec)
  }));

  const counts = summarizedSpecs.reduce((summary, spec) => {
    summary.total += 1;
    summary[spec.status] = (summary[spec.status] ?? 0) + 1;
    return summary;
  }, { total: 0, passed: 0, failed: 0, flaky: 0, skipped: 0, unknown: 0 });

  const totalDuration = summarizedSpecs.reduce((sum, spec) => sum + spec.duration, 0);
  const generatedAt = new Date().toISOString();
  const rows = summarizedSpecs.length > 0
    ? summarizedSpecs.map((spec) => `| ${escapeTableCell(spec.status)} | ${escapeTableCell(spec.file)}:${escapeTableCell(spec.line)} | ${escapeTableCell(spec.title)} | ${escapeTableCell(formatDuration(spec.duration))} |`).join('\n')
    : '| n/a | n/a | No specs found | 0.00s |';
  const failedSpecs = summarizedSpecs.filter((spec) => spec.status === 'failed' || spec.status === 'flaky');
  const failureSection = failedSpecs.length === 0
    ? '## Failure Details\n\n- None\n'
    : ['## Failure Details', '', ...failedSpecs.map((spec) => `### ${spec.title}\n- **File:** \`${spec.file}:${spec.line}\`\n- **Status:** ${spec.status}\n- **Error:** ${spec.error || 'Unknown error'}\n`)].join('\n');

  const markdown = [
    '# E2E Test Summary',
    '',
    `- **Generated at:** ${generatedAt}`,
    `- **Total specs:** ${counts.total}`,
    `- **Passed:** ${counts.passed}`,
    `- **Failed:** ${counts.failed}`,
    `- **Flaky:** ${counts.flaky}`,
    `- **Skipped:** ${counts.skipped}`,
    `- **Unknown:** ${counts.unknown}`,
    `- **Total duration:** ${formatDuration(totalDuration)}`,
    `- **HTML report:** \`test-results/html/index.html\``,
    `- **JSON report:** \`test-results/results.json\``,
    `- **JUnit report:** \`test-results/results.xml\``,
    '',
    '## Spec Results',
    '',
    '| Status | File | Scenario | Duration |',
    '| :--- | :--- | :--- | ---: |',
    rows,
    '',
    failureSection,
    ''
  ].join('\n');

  await mkdir(resultsDir, { recursive: true });
  await writeFile(summaryPath, markdown, 'utf8');
  process.stdout.write(`Wrote E2E summary to ${summaryPath}\n`);
}

const currentFilePath = fileURLToPath(import.meta.url);
const isDirectRun = process.argv[1] && path.resolve(process.argv[1]) === path.resolve(currentFilePath);

if (isDirectRun) {
  generateSummary().catch((error) => {
    process.stderr.write(`${error.message}\n`);
    process.exit(1);
  });
}
