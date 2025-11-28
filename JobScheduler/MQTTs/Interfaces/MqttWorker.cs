using Common.Models;
using Common.Models.Queues;
using Data.Repositorys.Bases;
using JOB.MQTTs.Interfaces;
using log4net;
using MQTTnet;
using MQTTnet.Formatter;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace JOB.MQTTs
{
    public class MqttWorker : IMqttWorker
    {
        private static readonly ILog _logger = LogManager.GetLogger("MQTT");

        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;

        private readonly Channel<MqttApplicationMessageReceivedEventArgs> _incomingMessageChannel;
        private readonly Channel<MqttApplicationMessage> _publishMessageChannel;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly ConcurrentDictionary<string, DateTime> _recentUUIDs = new();

        public MqttWorker()
        {
            _incomingMessageChannel = Channel.CreateUnbounded<MqttApplicationMessageReceivedEventArgs>();
            _publishMessageChannel = Channel.CreateBounded<MqttApplicationMessage>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait // 큐가 가득 차면 발행자가 대기하도록 설정
            });

            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            string clientId = ConfigData.mQTTSetting.id;
            string serverhost = ConfigData.mQTTSetting.host;
            string serverport = ConfigData.mQTTSetting.port;

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(serverhost, int.Parse(serverport))
                .WithCleanSession()
                .Build();

            //_mqttOptions = new MqttClientOptionsBuilder()
            //    .WithClientId("MyMqttClient")
            //    .WithTcpServer("localhost", 1883)
            //    //.WithTcpServer("broker.hivemq.com", 1883)
            //    //.WithTcpServer("test.mosquitto.org", 1883)
            //    .WithCleanSession()
            //    .Build();

            _mqttClient.ConnectedAsync += async e =>
            {
               
                _logger.Info("MQTT connected.");

                //구독 정보 입력
                foreach (var subscribe in ConfigData.SubscribeTopics.ToList())
                {
                    await _mqttClient.SubscribeAsync(subscribe.topic);
                }

                //foreach (var topic in subscribeTopics)
                //{
                //    await _mqttClient.SubscribeAsync(topic);
                //}
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.Info($"MQTT disconnected: {e.Reason}. Trying to reconnect...");
                // CancellationToken이 요청되기 전까지 5초 간격으로 재연결 시도
                await Task.Delay(5000, _cancellationTokenSource.Token);
                try
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        await _mqttClient.ConnectAsync(_mqttOptions, _cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Info($"Reconnect failed: {ex.Message}");
                }
            };

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                if (!_incomingMessageChannel.Writer.TryWrite(e))
                {
                    _logger.Info("FAILED to write to incoming message channel. It might be closed.");
                }
                return Task.CompletedTask;
            };
        }

        public async Task PublishAsync(string topic, string payload)
        {
            bool retain = false;
            ////retain 적용해야 하는 특정 Topic
            //string retainTopic = "acs/elevator/NO1/status";

            //// 현재 Topic이 retain 토픽인지 체크
            //if (topic == retainTopic)
            //{
            //    retain = true;
            //}

            var message = new MqttApplicationMessageBuilder()
                // 메시지를 보낼 Topic 지정
                // 예: "acs/worker/123/state"
                .WithTopic(topic)
                // 메시지 Payload(내용) 설정
                // byte[] 또는 string → 브로커로 전달될 실제 데이터
                .WithPayload(payload)
                // QoS(전달품질) 설정: ExactlyOnce
                // - AtMostOnce(0): 최대 1번 (빠르지만 유실 가능)
                // - AtLeastOnce(1): 최소 1번 (중복 가능)
                // - ExactlyOnce(2): 정확히 1번 (가장 안정적, 속도는 느림)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                // Retain(리테인) 플래그 설정
                // false = retain 사용 안 함 (메시지 보관 X)
                // true  = retain 사용 (브로커가 마지막 메시지를 저장)
                .WithRetainFlag(retain)
                .Build();

            await _publishMessageChannel.Writer.WriteAsync(message, _cancellationTokenSource.Token);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // 수신 메시지를 처리하는 백그라운드 작업을 시작
            _ = Task.Run(() => ProcessIncomingMessageQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            // 발행 큐를 처리하는 백그라운드 작업을 시작합니다.
            _ = Task.Run(() => ProcessPublishMessageQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            try
            {
                _logger.Info("Trying to connect...");
                await _mqttClient.ConnectAsync(_mqttOptions, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.Info($"Initial MQTT connection failed: {ex.Message}");
            }
        }

        private async Task ProcessIncomingMessageQueueAsync(CancellationToken stoppingToken)
        {
            // Channel에서 메시지를 비동기적으로 읽어옵니다.
            await foreach (var args in _incomingMessageChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var topic = args.ApplicationMessage.Topic;
                    var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

                    // UUID 필터링 로직
                    if (TryGetUuidFromPayload(payload, out string uuid))
                    {
                        if (_recentUUIDs.ContainsKey(uuid))
                        {
                            _logger.Info($"Duplicate message detected. Skipping: {uuid}");
                            continue; // 다음 메시지 처리로 넘어감
                        }
                        _recentUUIDs.TryAdd(uuid, DateTime.Now);
                        RemoveOldUuids(); // 오래된 UUID 정리
                    }
                    // 최종 목적지인 messageQueue에 Enqueue
                    QueueStorage.MqttEnqueueSubscribe(new MqttSubscribeMessageDto
                    {
                        topic = topic,
                        Payload = payload,
                        Timestamp = DateTime.Now,
                    });

                    _logger.Info($"Incoming Message: topic=[{topic}] payload={payload.BeautifyJson()}");
                }
                catch (Exception ex)
                {
                    _logger.Info($"Error processing received message: {ex.Message}\nPayload: {Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}");
                }
            }
        }

        private async Task ProcessPublishMessageQueueAsync_0(CancellationToken stoppingToken) // 발행실패시 메시지를 다시 큐에 넣는다
        {
            // Channel에서 메시지를 비동기적으로 읽어옵니다. 데이터가 없으면 대기합니다.
            await foreach (var message in _publishMessageChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // MQTT 클라이언트가 연결되어 있을 때만 발행을 시도합니다.
                    if (_mqttClient.IsConnected)
                    {
                        await _mqttClient.PublishAsync(message, stoppingToken);
                        _logger.Info($"Published Message: topic=[{message.Topic}] payload={Encoding.UTF8.GetString(message.Payload).BeautifyJson()}");
                    }
                    else
                    {
                        _logger.Info($"Client not connected. Re-queuing message. [{message.Topic}]");
                        // 연결이 끊겼다면 메시지를 다시 큐에 넣어 나중에 처리되도록 합니다.
                        await _publishMessageChannel.Writer.WriteAsync(message, stoppingToken);
                        // 연결이 복구될 때까지 잠시 대기
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.Info(ex.Message + " | FAILED to publish from queue. Requeuing...");
                    await _publishMessageChannel.Writer.WriteAsync(message, stoppingToken);
                }
            }
        }

        private async Task ProcessPublishMessageQueueAsync(CancellationToken stoppingToken) // 발행실패시 재시도 3번하고 메시지를 버린다
        {
            // Channel에서 메시지를 비동기적으로 읽어옵니다. 데이터가 없으면 대기합니다.
            await foreach (var message in _publishMessageChannel.Reader.ReadAllAsync(stoppingToken))
            {
                int retry = 0;
                const int maxRetry = 3;

                while (retry < maxRetry)
                {
                    try
                    {
                        // MQTT 클라이언트가 연결되어 있을 때만 발행을 시도합니다.
                        if (_mqttClient.IsConnected)
                        {
                            await _mqttClient.PublishAsync(message, stoppingToken);
                            _logger.Info($"Published Message: topic=[{message.Topic}] payload={Encoding.UTF8.GetString(message.Payload).BeautifyJson()}");
                            break; // 성공적으로 발행되면 루프를 종료
                        }
                        else
                        {
                            _logger.Info($"Client not connected. Delay.... [{message.Topic}]");
                            // 연결이 복구될 때까지 잠시 대기
                            await Task.Delay(3000, stoppingToken); // 연결 재시도 딜레이
                            retry++; // 연결 재시도까지 횟수에 포함
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        retry++;
                        _logger.Info($"{ex.Message} | FAILED to publish from queue. Retry {retry}/{maxRetry}...");
                        await Task.Delay(3000, stoppingToken); // 전송 재시도 딜레이
                    }
                }

                // 최대 재시도 횟수를 초과한 경우 메시지를 버립니다.
                if (retry >= maxRetry)
                {
                    _logger.Info($"Max retries reached for message: [{message.Topic}] Discarding message.");
                }
            }
        }

        private bool TryGetUuidFromPayload(string payload, out string uuid)
        {
            uuid = null!;
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("uuid", out var uuidProperty))
                {
                    uuid = uuidProperty.GetString()!;
                    return !string.IsNullOrWhiteSpace(uuid);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }

            return false;
        }

        private void RemoveOldUuids()
        {
            var threshold = DateTime.Now.AddMinutes(-10);
            foreach (var kvp in _recentUUIDs.ToArray())
            {
                if (kvp.Value < threshold)
                {
                    _recentUUIDs.TryRemove(kvp.Key, out _);
                }
            }
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}