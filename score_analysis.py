import random
import numpy as np
import matplotlib.pyplot as plt

# def point(p):
#     return random.random() < p

# def game(p):
#     score1 = 0
#     score2 = 0
#     while score1 < 25 and score2 < 25:
#         if point(p):
#             score1 += 1
#         else:
#             score2 += 1
#     return score1 > score2


# def test(p, n):
#     return np.mean([game(p) for _ in range(n)])

# def binomial_coefficient(n, k): #
#     return np.math.factorial(n) / (np.math.factorial(k) * np.math.factorial(n-k))

# def analytical(p):
#     n = 25
#     r1 = p**n * np.sum([binomial_coefficient(k+n-1, k) * (1-p)**k for k in range(n)])
#     r2 = p**n * np.sum([binomial_coefficient(l-1, n-1) * (1-p)**(l-n) for l in range(n, 2*n)])
#     r3 = p**n * np.math.factorial(n-1) * np.math.exp((1-p)*(n-1))
#     print("r1: " + str(r1))
#     print("r2: " + str(r2))
#     print("r3: " + str(r3))


# p_list = np.linspace(0, 1, 30)

# score_list = [test(p, 10000) for p in p_list]

# analytical_list = [analytical(p) for p in p_list]

# plt.figure()
# plt.plot(p_list, score_list, 'x', label='data')
# plt.plot(p_list, analytical_list, label='analytical')
# plt.xlabel('Chance of winning a point')
# plt.ylabel('Chance of winning the game')
# plt.title('Chance of winning the game as a function of the chance of winning a point')
# plt.legend()
# plt.show()

# exit()

#find all file in score_logs folder
import os
log_files = os.listdir('score_logs')
#labels end in .txt, so remove the .txt
labels = [x[5:-4] for x in log_files]
#replace _ with space for better readability
labels = [x.replace('_', ' ') for x in labels]
max_iter = 800
useMaxIter = True 

plt.figure()

for log_file, label in zip(log_files, labels):
    log_path = 'score_logs/' + log_file

    #read logs file
    scores = []
    with open(log_path, 'r') as f:
        lines = f.readlines()
        for line in lines:
            if line.startswith('Blue') or line.startswith('Purple'):
                #remove newline character
                line = line.rstrip()
                #split line into list of strings
                line = line.split()
                #convert strings to integers
                blueScore = int(line[1])
                purpleScore = int(line[4])
                #add tuple of scores to list
                if purpleScore == 0:
                    purpleScore = 1
                scores.append((blueScore, purpleScore))

    #Who win more games of 25 points?
    blueGameScore = 0
    purpleGameScore = 0
    blueWins = 0
    purpleWins = 0

    for i, score in enumerate(scores[1:]):
        if blueGameScore >= 25 or purpleGameScore >= 25:
            if blueGameScore > purpleGameScore:
                blueWins += 1
            else:
                purpleWins += 1
            blueGameScore = 0
            purpleGameScore = 0
        if score[0] - scores[i-1][0] > 0:
            blueGameScore += 1
        else:
            purpleGameScore += 1

    print(label)
    print("Chance of winning a point: " + str(scores[-1][0]/(scores[-1][0] + scores[-1][1])))
    print('Blue wins: ' + str(blueWins))
    print('Purple wins: ' + str(purpleWins)) 
    print('') 

    #plot the ratio of blue to purple
    if useMaxIter:
        plt.plot([x[0]/x[1] for x in scores[:max_iter]], label=label)
    else:
        plt.plot([x[0]/x[1] for x in scores], label=label)
plt.xlabel('Number of points played')
plt.ylabel('Ratio of blue score to purple score')
plt.title('Ratio of blue score to purple score over time')
plt.legend()
plt.show()

