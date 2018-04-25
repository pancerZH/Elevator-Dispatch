#include<stdio.h>
#include<stdlib.h>
#include<pthread.h>
#include<semaphore.h>

#define TIMES 10  // 每个线程执行10次

int* buffer;  // buffer的起始位置将存放写指针的位置，buffer的末尾存放读指针位置
int buffer_size = 0;
sem_t mutex, empty, full;

sem_t producer_main_mutex, consumer_main_mutex;  // 阻塞main函数

// 打印buffer内容和操作的线程
void printAll(const int No)
{
	for(int i=1; i<buffer_size-1; i++)
	{
		printf("%d ", buffer[i]);
	}
	printf("\n");
	if(No > 0)  // 生产者
		printf("Producer # %d is working\n\n", No);
	else  // 消费者
		printf("Consumer # %d is working\n\n", 0-No);
}

// 生产者函数，要求传入编号
void* producer(void* para)
{
	int No = (int*)para;  // 线程的编号
	int times = TIMES;
	while(times--)
	{
		sem_wait(&empty);  // buffer未满
		sem_wait(&mutex);  // 拿到buffer的写入权
		int position = *buffer;
		buffer[position++] = No;
		printAll(No);
		if(position == buffer_size-1)
			*buffer = 1;
		else
			*buffer = position;
		sem_post(&mutex);
		sem_post(&full);

		sleep(1);
	}
	sem_post(&producer_main_mutex);
	pthread_exit(0);
}

// 消费者函数，要求传入编号
void* consumer(void* para)
{
	int No = (int*)para;
	int times = TIMES;
	while(times--)
	{
		sem_wait(&full);  // buffer不空
		sem_wait(&mutex);  // 拿到buffer的读取权
		int position = *(buffer+buffer_size-1);
		buffer[position++] = 0;
		printAll(0-No);
		if(position == buffer_size-1)
			*(buffer+buffer_size-1) = 1;
		else
			*(buffer+buffer_size-1) = position;
		sem_post(&mutex);
		sem_post(&empty);

		sleep(1);
	}
	sem_post(&consumer_main_mutex);
	pthread_exit(0);
}

int main(int argc, char** argv)
{
	// 要求第一个参数为生产者的数量
	// 第二个参数为消费者数量，第三个为buffer的大小
	if(argc != 4)
	{
		perror("输入参数数量应为3个正整数，请重试！\n");
		exit(1);
	}

	int producer_num, consumer_num;
	sscanf(argv[1], "%d", &producer_num);
	sscanf(argv[2], "%d", &consumer_num);
	sscanf(argv[3], "%d", &buffer_size);
	buffer_size += 2;

	buffer = malloc(buffer_size*sizeof(int));
	for(int i=1; i<buffer_size-1; i++)
	{
		buffer[i] = 0;
	}
	*buffer = *(buffer+buffer_size-1) = 1;
	sem_init(&mutex, 0, 1);
	sem_init(&empty, 0, buffer_size-2);
	sem_init(&full, 0, 0);
	sem_init(&producer_main_mutex, 0, producer_num);
	sem_init(&consumer_main_mutex, 0, consumer_num);

	for(int i=1; i<=producer_num; ++i)
	{
		pthread_t tid;
		pthread_create(&tid, NULL, producer, (void*)i);
		sem_wait(&producer_main_mutex);
	}
	for(int i=1; i<=consumer_num; ++i)
	{
		pthread_t tid;
		pthread_create(&tid, NULL, consumer, (void*)i);
		sem_wait(&consumer_main_mutex);
	}
	
	if(consumer_num < producer_num)
		for(int i=0; i<consumer_num; ++i)
			sem_wait(&consumer_main_mutex);
	else if(consumer_num > producer_num)
		for(int i=0; i<producer_num; ++i)
			sem_wait(&producer_main_mutex);
	else
		for(int i=0; i<consumer_num; ++i)
		{
			sem_wait(&consumer_main_mutex);
			sem_wait(&producer_main_mutex);
		}
	free(buffer);
	return 0;
}
