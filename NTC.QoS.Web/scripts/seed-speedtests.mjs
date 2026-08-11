#!/usr/bin/env node

/**
 * Speedtest Database Seeder (Node.js Version)
 * 
 * Seeds the database with millions of random speedtest results.
 * Respects API rate limit: 100 requests / 10 seconds
 * 
 * Usage:
 *   node scripts/seed-speedtests.mjs [options]
 *   
 * Options:
 *   --count, -n      Total speedtests to create (default: 1000)
 *   --url, -u        API base URL (default: http://localhost:5000)
 *   --token, -t      Authorization token
 *   --batch, -b      Requests per rate limit window (default: 100)
 *   --wait, -w       Wait time in seconds between batches (default: 10)
 *   --concurrency    Concurrent requests (default: 20)
 *   --help, -h       Show help
 */

import https from 'https';
import http from 'http';

// ============================================================================
// Configuration
// ============================================================================

const config = {
  totalCount: 1000,
  apiBaseUrl: process.env.NEXT_PUBLIC_QOS_BACKEND_URL || 'http://localhost:5000',
  authToken: '',
  batchSize: 100,
  waitTime: 10, // seconds
  concurrency: 20,
};

// Parse command line arguments
const args = process.argv.slice(2);
for (let i = 0; i < args.length; i++) {
  switch (args[i]) {
    case '--count':
    case '-n':
      config.totalCount = parseInt(args[++i], 10);
      break;
    case '--url':
    case '-u':
      config.apiBaseUrl = args[++i];
      break;
    case '--token':
    case '-t':
      config.authToken = args[++i];
      break;
    case '--batch':
    case '-b':
      config.batchSize = parseInt(args[++i], 10);
      break;
    case '--wait':
    case '-w':
      config.waitTime = parseInt(args[++i], 10);
      break;
    case '--concurrency':
      config.concurrency = parseInt(args[++i], 10);
      break;
    case '--help':
    case '-h':
      console.log(`
Speedtest Database Seeder (Node.js Version)

Usage: node scripts/seed-speedtests.mjs [options]

Options:
  --count, -n      Total speedtests to create (default: 1000)
  --url, -u        API base URL (default: http://localhost:5000)
  --token, -t      Authorization token
  --batch, -b      Requests per rate limit window (default: 100)
  --wait, -w       Wait time in seconds between batches (default: 10)
  --concurrency    Concurrent requests (default: 20)
  --help, -h       Show this help

Examples:
  node scripts/seed-speedtests.mjs -n 10000 -u http://localhost:5000
  node scripts/seed-speedtests.mjs -n 1000000 -t "your-jwt-token" --concurrency 50
      `);
      process.exit(0);
  }
}

// ============================================================================
// Constants
// ============================================================================

const PHONE_MODELS = [
  'Samsung Galaxy S24 Ultra',
  'Samsung Galaxy S23',
  'Samsung Galaxy A54',
  'iPhone 15 Pro Max',
  'iPhone 15',
  'iPhone 14',
  'iPhone 13',
  'Xiaomi 14',
  'Xiaomi Redmi Note 13',
  'OPPO Reno 11',
  'OPPO A98',
  'Vivo V30',
  'Vivo Y36',
  'Realme 12 Pro+',
  'Realme C55',
  'Huawei P60',
  'Google Pixel 8',
  'OnePlus 12',
  'Nothing Phone 2',
  'Infinix Note 30',
];

const SIGNAL_QUALITIES = ['Excellent', 'Good', 'Average', 'Weak', 'VeryWeak', 'Unknown'];

// Philippine MNCs
const MNCS = ['02', '03', '05', '18'];

// Philippine geographic bounds
const GEO_BOUNDS = {
  minLat: 4.5,
  maxLat: 21.2,
  minLng: 116.0,
  maxLng: 127.0,
};

// Reference data from API
let serviceProviderIds = [];
let technologyIds = [];
let generationIds = [];

// ============================================================================
// Utilities
// ============================================================================

const colors = {
  reset: '\x1b[0m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m',
};

function log(level, message) {
  const timestamp = new Date().toISOString();
  const color = {
    INFO: colors.blue,
    SUCCESS: colors.green,
    WARNING: colors.yellow,
    ERROR: colors.red,
  }[level] || colors.reset;
  console.log(`[${timestamp}] [${color}${level}${colors.reset}] ${message}`);
}

function randomFloat(min, max, precision = 2) {
  return parseFloat((Math.random() * (max - min) + min).toFixed(precision));
}

function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randomElement(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}

function randomDate() {
  const now = new Date();
  const daysAgo = randomInt(0, 730);
  const date = new Date(now.getTime() - daysAgo * 24 * 60 * 60 * 1000);
  date.setHours(randomInt(0, 23), randomInt(0, 59), randomInt(0, 59));
  return date.toISOString();
}

function calculateSignalQuality(rssi) {
  if (rssi >= -65) return 'Excellent';
  if (rssi >= -75) return 'Good';
  if (rssi >= -85) return 'Average';
  if (rssi >= -95) return 'Weak';
  if (rssi >= -110) return 'VeryWeak';
  return 'Unknown';
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function showProgress(current, total, startTime) {
  const percentage = Math.floor((current / total) * 100);
  const elapsed = (Date.now() - startTime) / 1000;
  const rate = current / elapsed;
  const remaining = (total - current) / rate;
  
  const width = 40;
  const filled = Math.floor((current / total) * width);
  const empty = width - filled;
  
  const bar = '█'.repeat(filled) + '░'.repeat(empty);
  const eta = remaining > 0 ? `ETA: ${formatTime(remaining)}` : '';
  
  process.stdout.write(
    `\r${colors.cyan}Progress: [${colors.green}${bar}${colors.cyan}] ` +
    `${percentage}% (${current}/${total}) | ${rate.toFixed(1)}/s | ${eta}${colors.reset}   `
  );
}

function formatTime(seconds) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  return `${h}h ${m}m ${s}s`;
}

// ============================================================================
// HTTP Client
// ============================================================================

function makeRequest(method, url, data = null) {
  return new Promise((resolve, reject) => {
    const parsedUrl = new URL(url);
    const isHttps = parsedUrl.protocol === 'https:';
    const httpModule = isHttps ? https : http;
    
    const options = {
      hostname: parsedUrl.hostname,
      port: parsedUrl.port || (isHttps ? 443 : 80),
      path: parsedUrl.pathname + parsedUrl.search,
      method,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    };
    
    if (config.authToken) {
      options.headers['Authorization'] = `Bearer ${config.authToken}`;
    }
    
    if (data) {
      const body = JSON.stringify(data);
      options.headers['Content-Length'] = Buffer.byteLength(body);
    }
    
    const req = httpModule.request(options, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        try {
          const json = body ? JSON.parse(body) : null;
          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve({ status: res.statusCode, data: json });
          } else {
            reject({ status: res.statusCode, data: json });
          }
        } catch (e) {
          reject({ status: res.statusCode, error: body });
        }
      });
    });
    
    req.on('error', reject);
    req.setTimeout(30000, () => {
      req.destroy();
      reject(new Error('Request timeout'));
    });
    
    if (data) {
      req.write(JSON.stringify(data));
    }
    req.end();
  });
}

// ============================================================================
// API Functions
// ============================================================================

async function fetchServiceProviders() {
  log('INFO', 'Fetching wireless service providers...');
  try {
    const result = await makeRequest('GET', `${config.apiBaseUrl}/service-providers?pageSize=1000&serviceTypes=wireless`);
    if (result.data?.data) {
      serviceProviderIds = result.data.data.map(p => p.id);
      log('SUCCESS', `Found ${serviceProviderIds.length} wireless service providers`);
    }
  } catch (e) {
    log('WARNING', 'Could not fetch service providers');
  }
}

async function fetchTechnologies() {
  log('INFO', 'Fetching technologies...');
  try {
    const result = await makeRequest('GET', `${config.apiBaseUrl}/technologies?pageSize=1000`);
    if (result.data?.data) {
      technologyIds = result.data.data.map(t => t.id);
      log('SUCCESS', `Found ${technologyIds.length} technologies`);
    }
  } catch (e) {
    log('WARNING', 'Could not fetch technologies');
  }
}

async function fetchGenerations() {
  log('INFO', 'Fetching generations...');
  try {
    const result = await makeRequest('GET', `${config.apiBaseUrl}/generations?pageSize=1000`);
    if (result.data?.data) {
      generationIds = result.data.data.map(g => g.id);
      log('SUCCESS', `Found ${generationIds.length} generations`);
    }
  } catch (e) {
    log('WARNING', 'Could not fetch generations');
  }
}

// ============================================================================
// Speedtest Generation
// ============================================================================

function generateSpeedtestPayload() {
  const lat = randomFloat(GEO_BOUNDS.minLat, GEO_BOUNDS.maxLat, 6);
  const lng = randomFloat(GEO_BOUNDS.minLng, GEO_BOUNDS.maxLng, 6);
  const rssi = randomInt(-110, -50);
  const signalQuality = calculateSignalQuality(rssi);
  
  // Speed varies by signal quality
  let maxDownload, maxUpload, maxPing;
  switch (signalQuality) {
    case 'Excellent':
      maxDownload = 500; maxUpload = 100; maxPing = 20;
      break;
    case 'Good':
      maxDownload = 200; maxUpload = 50; maxPing = 50;
      break;
    case 'Average':
      maxDownload = 50; maxUpload = 20; maxPing = 100;
      break;
    case 'Weak':
      maxDownload = 10; maxUpload = 5; maxPing = 200;
      break;
    default:
      maxDownload = 5; maxUpload = 2; maxPing = 500;
  }
  
  const payload = {
    cellIdentifier: randomInt(0, 268435455).toString(16).toUpperCase().padStart(8, '0'),
    latitude: lat,
    longitude: lng,
    mcc: '515',
    mnc: randomElement(MNCS),
    phoneModel: randomElement(PHONE_MODELS),
    tac: randomInt(0, 65535).toString().padStart(5, '0'),
    dateTime: randomDate(),
    ping: randomFloat(5, maxPing, 2),
    jitter: randomFloat(0.5, 50, 2),
    rssi,
    uploadSpeed: randomFloat(0.1, maxUpload, 2),
    downloadSpeed: randomFloat(0.5, maxDownload, 2),
    signalQuality,
    isFixedBroadband: false,
  };
  
  if (serviceProviderIds.length > 0) {
    payload.serviceProviderId = randomElement(serviceProviderIds);
  }
  
  if (technologyIds.length > 0) {
    payload.technologyId = randomElement(technologyIds);
  }
  
  if (generationIds.length > 0) {
    payload.generationId = randomElement(generationIds);
  }
  
  return payload;
}

async function createSpeedtest(payload) {
  return makeRequest('POST', `${config.apiBaseUrl}/speedtests`, payload);
}

// ============================================================================
// Batch Processing with Concurrency Control
// ============================================================================

async function processBatch(startIndex, batchSize, total, startTime) {
  const endIndex = Math.min(startIndex + batchSize, total);
  const tasks = [];
  let successCount = 0;
  let failCount = 0;
  
  // Create all payloads first
  const payloads = [];
  for (let i = startIndex; i < endIndex; i++) {
    payloads.push(generateSpeedtestPayload());
  }
  
  // Process with concurrency limit
  const concurrencyLimit = config.concurrency;
  let currentIndex = 0;
  
  const processNext = async () => {
    while (currentIndex < payloads.length) {
      const idx = currentIndex++;
      const payload = payloads[idx];
      
      try {
        await createSpeedtest(payload);
        successCount++;
      } catch (e) {
        failCount++;
      }
      
      showProgress(startIndex + idx + 1, total, startTime);
    }
  };
  
  // Start concurrent workers
  const workers = [];
  for (let i = 0; i < concurrencyLimit; i++) {
    workers.push(processNext());
  }
  
  await Promise.all(workers);
  
  return { success: successCount, fail: failCount };
}

// ============================================================================
// Main Execution
// ============================================================================

async function main() {
  console.log(`
${colors.cyan}╔══════════════════════════════════════════════════════════════╗
║        ${colors.green}Speedtest Database Seeder (Node.js)${colors.cyan}                  ║
║        ${colors.yellow}Rate Limit: 100 req / 10 sec${colors.cyan}                         ║
╚══════════════════════════════════════════════════════════════╝${colors.reset}
  `);
  
  log('INFO', 'Configuration:');
  log('INFO', `  API URL: ${config.apiBaseUrl}`);
  log('INFO', `  Total records: ${config.totalCount.toLocaleString()}`);
  log('INFO', `  Batch size: ${config.batchSize}`);
  log('INFO', `  Wait time: ${config.waitTime}s`);
  log('INFO', `  Concurrency: ${config.concurrency}`);
  console.log();
  
  // Test API connection
  log('INFO', 'Testing API connection...');
  try {
    await makeRequest('GET', `${config.apiBaseUrl}/service-providers?pageSize=1`);
    log('SUCCESS', 'API connection successful');
  } catch (e) {
    log('ERROR', `Cannot connect to API at ${config.apiBaseUrl}`);
    log('ERROR', 'Please check the API URL and authentication token.');
    process.exit(1);
  }
  
  // Fetch reference data
  await fetchServiceProviders();
  await fetchTechnologies();
  await fetchGenerations();
  
  if (technologyIds.length === 0) {
    log('ERROR', 'No technologies available. Technology ID is required.');
    process.exit(1);
  }
  
  console.log();
  log('INFO', `Starting to seed ${config.totalCount.toLocaleString()} speedtest records...`);
  console.log();
  
  const startTime = Date.now();
  let totalSuccess = 0;
  let totalFail = 0;
  const totalBatches = Math.ceil(config.totalCount / config.batchSize);
  
  for (let i = 0; i < config.totalCount; i += config.batchSize) {
    const batchNum = Math.floor(i / config.batchSize) + 1;
    const batchSize = Math.min(config.batchSize, config.totalCount - i);
    
    const result = await processBatch(i, batchSize, config.totalCount, startTime);
    totalSuccess += result.success;
    totalFail += result.fail;
    
    // Rate limit: wait between batches
    if (i + config.batchSize < config.totalCount) {
      console.log();
      log('INFO', `Batch ${batchNum}/${totalBatches} complete. Waiting ${config.waitTime}s for rate limit...`);
      await sleep(config.waitTime * 1000);
    }
  }
  
  const duration = (Date.now() - startTime) / 1000;
  
  console.log(`

${colors.cyan}╔══════════════════════════════════════════════════════════════╗
║                     ${colors.green}Seeding Complete!${colors.cyan}                        ║
╚══════════════════════════════════════════════════════════════╝${colors.reset}
  `);
  
  log('SUCCESS', `Total records created: ${totalSuccess.toLocaleString()}`);
  if (totalFail > 0) {
    log('WARNING', `Failed records: ${totalFail.toLocaleString()}`);
  }
  log('INFO', `Time elapsed: ${formatTime(duration)}`);
  log('INFO', `Average rate: ${(totalSuccess / duration).toFixed(2)} records/second`);
  console.log();
}

main().catch(err => {
  log('ERROR', err.message || err);
  process.exit(1);
});
