import math
from scipy.stats import entropy


class BNode:
    def __init__(self, label, time, parent, depth):
        self.label = label
        self.time = time
        self.visits = 1
        self.parent = parent
        self.children = {}  # key=child label, val=BNode
        self.depth = depth
        self.m2 = 0

    def update_time(self, time):
        d = self.time + (time-self.time)/self.visits
        self.m2 += (time-self.time)*(time-d)
        self.time = d

    def get_deepest_path(self):
        if len(self.children) == 0: return None
        max_d = 0
        best_n = None
        for c in self.children:
            if self.children[c].depth > max_d:
                max_d = self.children[c].depth
                best_n = self.children[c]
        return best_n

    def frequency(self, log=True):
        f = (self.visits / self.parent.visits) if self.parent else 1
        if log and self.parent:
            return math.log1p(f)  # natural log eg log base e
        else:
            return f

    def update_depth(self):
        self.depth += 1
        if self.parent:
            self.parent.update_depth()

    def find_common(self, other, path):
        n = filter(lambda x: x == other.label, self.children.keys())
        path.append(dict(time=self.time, frequency=self.frequency()))
        if len(n) == 0:
            return path
        b_path = []
        for c in n:
            p = []
            p = self.children[c].find_common(other.children[c], p)
            if len(p) > len(b_path):
                b_path = p
        path.extend(b_path)
        return path

    def update_self(self, chain):
        """chain=list(dict(label,time))"""
        data = chain.pop(0)
        self.visits += 1
        self.update_time(data['time'])
        if not chain:
            return
        n = chain[0]['label']
        if n in self.children:
            self.children[n].update_self(chain)
        else:
            if len(self.children) == 0:
                self.update_depth()
            c = BNode(n, chain[0]['time'], self, 0)
            self.children[n] = c
            c.update_self(chain)


class BTree:
    def __init__(self):
        self.paths = {}

    def build(self, chain):
        n = chain[0]
        l = n['label']
        if l in self.paths:
            self.paths[l].update_self(chain)
        else:
            r = BNode(l, n['time'], None, 0)
            self.paths[l] = r
            r.update_self(chain)

    def longest_path(self):
        cur = None
        d = 0
        for p in self.paths:
            if self.paths[p].depth > d:
                d = self.paths[p].depth
                cur = self.paths[p]
        path = []
        if cur == None:
            return None
        path.append(dict(time=cur.time, frequency=cur.frequency()))
        while cur is not None:
            old_cur = cur
            cur = cur.get_deepest_path()
            if cur:
                path.append(dict(time=cur.time, frequency=cur.frequency()))
            else:
                path.append(dict(time=old_cur.time,frequency=old_cur.frequency()))
        return path

    def lcp(self, other):
        cur = filter(lambda x: x in other.paths.keys(), self.paths.keys())
        best_p = []
        for n in cur:
            pn = self.paths[n]
            p = [dict(time=pn.time,frequency=pn.frequency())]
            p = self.paths[n].find_common(other.paths[n], p)
            if len(p) > len(best_p):
                best_p = p
        return best_p


def ic(path, weight):
    return entropy([p[weight] for p in path])


def lin(a, b, w):
    p1 = a.longest_path()
    p2 = b.longest_path()
    if p1 is None or p2 is None:
        return 0
    lcp = a.lcp(b)
    return 2 * ic(lcp, w) / (ic(p1, w) + ic(p2, w))


if __name__ == "__main__":
    b1 = BTree()
    b1.build([{"time": 10, "label": "a"},
              {"time": 13, "label": "b"},
              {"time": 2, "label": "c"}])
    b2 = BTree()
    b2.build([{"time": 1, "label": "a"},
              {"time": 2, "label": "b"}])
    print lin(b1, b2, "frequency")
