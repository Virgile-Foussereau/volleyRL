import matplotlib.pyplot as plt

#read logs file
scores = []
with open('logs_RL_vs_Baseline.txt', 'r') as f:
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

print('Blue wins: ' + str(blueWins))
print('Purple wins: ' + str(purpleWins))  

#plot the ratio of blue to purple
plt.figure()
plt.plot([x[0]/x[1] for x in scores])
plt.xlabel('Number of points played')
plt.ylabel('Ratio of blue score to purple score')
plt.title('Ratio of blue score to purple score over time')
plt.show()

